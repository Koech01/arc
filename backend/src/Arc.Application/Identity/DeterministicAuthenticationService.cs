using Arc.Domain.Models;
using Arc.Domain.Exceptions;
using Arc.Application.Admin;
using Microsoft.Extensions.Logging;


namespace Arc.Application.Identity;
/// <summary>
/// Deterministic authentication service that handles user registration and authentication.
/// Enforces per-account lockout after repeated failed logins, records login history,
/// and checks soft-deleted state before allowing access.
/// </summary>
public sealed class DeterministicAuthenticationService : IAuthenticationService
{
    private const int LockoutThreshold = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ILoginHistoryRepository _loginHistory;
    private readonly ILogger<DeterministicAuthenticationService> _logger;

    public DeterministicAuthenticationService(
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        ILoginHistoryRepository loginHistory,
        ILogger<DeterministicAuthenticationService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHashingService = passwordHashingService ?? throw new ArgumentNullException(nameof(passwordHashingService));
        _loginHistory = loginHistory ?? throw new ArgumentNullException(nameof(loginHistory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<User> RegisterAsync(string username, string email, string password, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long", nameof(password));

        var normalizedEmail = email.ToLowerInvariant();

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", normalizedEmail);
            throw AuthenticationException.DuplicateEmail(normalizedEmail);
        }

        var passwordHash = _passwordHashingService.HashPassword(password);
        var user = User.Create(username, normalizedEmail, passwordHash, role);
        var createdUser = await _userRepository.CreateAsync(user);

        _logger.LogInformation("User registered successfully: {UserId}, Username: {Username}, Email: {Email}, Role: {Role}",
            createdUser.Id, createdUser.Username, createdUser.Email, createdUser.Role);

        return createdUser;
    }

    // Overload for test compatibility (omits role, defaults to UserRole.User)
    public async Task<User> RegisterAsync(string email, string password)
        => await RegisterAsync(email, email, password, UserRole.User);

    public async Task<User> AuthenticateAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        var normalizedEmail = email.ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (user is null)
        {
            _logger.LogWarning("Authentication attempt with non-existent email: {Email}", normalizedEmail);
            throw AuthenticationException.InvalidCredentials();
        }

        // Soft-deleted accounts must not be accessible
        if (user.IsDeleted)
        {
            _logger.LogWarning("Authentication attempt on deleted account: {UserId}", user.Id);
            throw AuthenticationException.AccountDeleted();
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Authentication attempt with inactive account: {UserId}", user.Id);
            throw AuthenticationException.InactiveAccount();
        }

        // Lockout check (re-evaluate in case the lockout window has expired)
        if (user.IsLockedOut)
        {
            _logger.LogWarning("Authentication attempt on locked account: {UserId}, LockedUntil: {LockedUntil}",
                user.Id, user.LockedUntilUtc);
            await _loginHistory.RecordAsync(user.Id, false, "AccountLocked");
            throw AuthenticationException.AccountLocked(user.LockedUntilUtc!.Value);
        }

        // If the lockout window has expired, reset the counter before checking the password
        if (user.LockedUntilUtc.HasValue && DateTime.UtcNow >= user.LockedUntilUtc.Value)
        {
            user = user.WithResetFailedAttempts();
            await _userRepository.UpdateAsync(user);
        }

        if (!_passwordHashingService.VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("Authentication attempt with invalid password: {UserId}, Attempt: {Attempts}",
                user.Id, user.FailedLoginAttempts + 1);

            var locked = user.WithFailedLoginAttempt(LockoutThreshold, LockoutDuration);
            await _userRepository.UpdateAsync(locked);
            await _loginHistory.RecordAsync(user.Id, false, "InvalidPassword");

            if (locked.IsLockedOut)
            {
                _logger.LogWarning("Account locked due to repeated failures: {UserId}", user.Id);
                throw AuthenticationException.AccountLocked(locked.LockedUntilUtc!.Value);
            }

            throw AuthenticationException.InvalidCredentials();
        }

        // Successful login: reset counter
        var authenticated = user.WithResetFailedAttempts();
        await _userRepository.UpdateAsync(authenticated);
        await _loginHistory.RecordAsync(authenticated.Id, true);

        _logger.LogInformation("User authenticated successfully: {UserId}", authenticated.Id);
        return authenticated;
    }

    public async Task<User?> GetUserByIdAsync(UserId userId)
        => await _userRepository.GetByIdAsync(userId);

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _userRepository.GetByEmailAsync(email.ToLowerInvariant());
    }

    public async Task<User> UpdateProfileAsync(UserId userId, string newUsername, string newEmail, string? newFirstname = null)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
            throw new ArgumentException("Username cannot be null or empty", nameof(newUsername));

        if (string.IsNullOrWhiteSpace(newEmail))
            throw new ArgumentException("Email cannot be null or empty", nameof(newEmail));

        var normalizedEmail = newEmail.ToLowerInvariant();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Profile update attempt for non-existent user: {UserId}", userId);
            throw AuthenticationException.UserNotFound();
        }

        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (existingUser is not null && existingUser.Id != userId)
        {
            _logger.LogWarning("Profile update attempt with duplicate email: {Email}", normalizedEmail);
            throw AuthenticationException.DuplicateEmail(normalizedEmail);
        }

        var updatedUser = user.UpdateProfile(newUsername, normalizedEmail, newFirstname);
        var result = await _userRepository.UpdateAsync(updatedUser);

        _logger.LogInformation("User profile updated: {UserId}, NewUsername: {Username}, NewEmail: {Email}",
            userId, newUsername, normalizedEmail);
        return result;
    }
}