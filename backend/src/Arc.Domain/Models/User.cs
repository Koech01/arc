namespace Arc.Domain.Models;


/// <summary>
/// Represents a user in the system with authentication and authorization capabilities.
/// This is a domain entity that enforces business rules around user identity and access.
/// </summary>
public sealed class User
{
    public UserId Id { get; }
    public string Username { get; }
    public string Email { get; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; }
    public string? Firstname { get; }
    public DateTime CreatedAt { get; }
    public bool IsActive { get; }
    public int FailedLoginAttempts { get; }
    public DateTime? LockedUntilUtc { get; }
    public DateTime? DeletedAt { get; }

    /// <summary>True when the account is under an active time-based lockout.</summary>
    public bool IsLockedOut => LockedUntilUtc.HasValue && DateTime.UtcNow < LockedUntilUtc.Value;

    /// <summary>True when the account has been soft-deleted.</summary>
    public bool IsDeleted => DeletedAt.HasValue;

    public User(
        UserId id,
        string username,
        string email,
        string passwordHash,
        UserRole role,
        DateTime createdAt,
        bool isActive = true,
        string? firstname = null,
        int failedLoginAttempts = 0,
        DateTime? lockedUntilUtc = null,
        DateTime? deletedAt = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));

        if (username.Length < 3 || username.Length > 50)
            throw new ArgumentException("Username must be between 3 and 50 characters", nameof(username));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be null or empty", nameof(passwordHash));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (!string.IsNullOrWhiteSpace(firstname) && firstname.Length > 100)
            throw new ArgumentException("First name cannot exceed 100 characters", nameof(firstname));

        if (failedLoginAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(failedLoginAttempts), "Failed login attempts cannot be negative");

        Id = id;
        Username = username;
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        Firstname = string.IsNullOrWhiteSpace(firstname) ? null : firstname.Trim();
        CreatedAt = createdAt;
        IsActive = isActive;
        FailedLoginAttempts = failedLoginAttempts;
        LockedUntilUtc = lockedUntilUtc;
        DeletedAt = deletedAt;
    }

    /// <summary>Creates a new user with a generated ID and current timestamp.</summary>
    public static User Create(string username, string email, string passwordHash, UserRole role)
    {
        return new User(
            UserId.From(Guid.NewGuid()),
            username,
            email,
            passwordHash,
            role,
            DateTime.UtcNow);
    }

    /// <summary>Creates a new user with a generated ID and current timestamp, omitting email (for test compatibility).</summary>
    public static User Create(string username, string passwordHash, UserRole role)
    {
        // Use username as email if it looks like an email, otherwise generate a dummy email
        var email = username.Contains("@") ? username : $"{username}@example.com";
        return Create(username, email, passwordHash, role);
    }

    /// <summary>
    /// Creates a dummy user for test compatibility (parameterless overload).
    /// </summary>
    public static User Create()
    {
        return new User(
            UserId.From(Guid.NewGuid()),
            "dummyuser",
            "dummy@example.com",
            "dummyhash",
            UserRole.User,
            DateTime.UtcNow,
            true,
            "Dummy",
            0,
            null,
            null
        );
    }

    /// <summary>Deactivates the user account.</summary>
    public User Deactivate()
    {
        return new User(Id, Username, Email, PasswordHash, Role, CreatedAt, false, Firstname, FailedLoginAttempts, LockedUntilUtc, DeletedAt);
    }

    /// <summary>Activates the user account.</summary>
    public User Activate()
    {
        return new User(Id, Username, Email, PasswordHash, Role, CreatedAt, true, Firstname, FailedLoginAttempts, LockedUntilUtc, DeletedAt);
    }

    /// <summary>Changes the user's role.</summary>
    public User UpdateRole(UserRole newRole)
    {
        return new User(Id, Username, Email, PasswordHash, newRole, CreatedAt, IsActive, Firstname, FailedLoginAttempts, LockedUntilUtc, DeletedAt);
    }

    /// <summary>
    /// Records a failed login attempt. Applies a lockout when the threshold is reached.
    /// </summary>
    /// <param name="maxAttempts">Number of consecutive failures before lockout.</param>
    /// <param name="lockoutDuration">Duration of the lockout window.</param>
    public User WithFailedLoginAttempt(int maxAttempts, TimeSpan lockoutDuration)
    {
        var newAttempts = FailedLoginAttempts + 1;
        var lockedUntil = newAttempts >= maxAttempts
            ? (DateTime?)DateTime.UtcNow.Add(lockoutDuration)
            : null;

        return new User(Id, Username, Email, PasswordHash, Role, CreatedAt, IsActive, Firstname, newAttempts, lockedUntil, DeletedAt);
    }

    /// <summary>Resets the failed login counter and removes any active lockout.</summary>
    public User WithResetFailedAttempts()
    {
        return new User(Id, Username, Email, PasswordHash, Role, CreatedAt, IsActive, Firstname, 0, null, DeletedAt);
    }

    /// <summary>Marks the user as soft-deleted (irreversible by normal operations).</summary>
    public User SoftDelete()
    {
        return new User(Id, Username, Email, PasswordHash, Role, CreatedAt, false, Firstname, FailedLoginAttempts, LockedUntilUtc, DateTime.UtcNow);
    }

    /// <summary>Updates the user's password hash.</summary>
    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be null or empty", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }

    /// <summary>Returns a new User with the password hash replaced (immutable variant).</summary>
    public User WithNewPasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be null or empty", nameof(newPasswordHash));

        return new User(Id, Username, Email, newPasswordHash, Role, CreatedAt, IsActive, Firstname, FailedLoginAttempts, LockedUntilUtc, DeletedAt);
    }

    /// <summary>Updates the user's email address.</summary>
    public User UpdateEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
            throw new ArgumentException("Email cannot be null or empty", nameof(newEmail));

        if (!IsValidEmail(newEmail))
            throw new ArgumentException("Invalid email format", nameof(newEmail));

        return new User(Id, Username, newEmail, PasswordHash, Role, CreatedAt, IsActive, Firstname, FailedLoginAttempts, LockedUntilUtc, DeletedAt);
    }

    /// <summary>Updates the user's username.</summary>
    public User UpdateUsername(string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
            throw new ArgumentException("Username cannot be null or empty", nameof(newUsername));

        if (newUsername.Length < 3 || newUsername.Length > 50)
            throw new ArgumentException("Username must be between 3 and 50 characters", nameof(newUsername));

        return new User(Id, newUsername, Email, PasswordHash, Role, CreatedAt, IsActive, Firstname, FailedLoginAttempts, LockedUntilUtc, DeletedAt);
    }

    /// <summary>Updates the user's profile (username, email, and firstname).</summary>
    public User UpdateProfile(string newUsername, string newEmail, string? newFirstname = null)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
            throw new ArgumentException("Username cannot be null or empty", nameof(newUsername));

        if (newUsername.Length < 3 || newUsername.Length > 50)
            throw new ArgumentException("Username must be between 3 and 50 characters", nameof(newUsername));

        if (string.IsNullOrWhiteSpace(newEmail))
            throw new ArgumentException("Email cannot be null or empty", nameof(newEmail));

        if (!IsValidEmail(newEmail))
            throw new ArgumentException("Invalid email format", nameof(newEmail));

        if (!string.IsNullOrWhiteSpace(newFirstname) && newFirstname.Length > 100)
            throw new ArgumentException("First name cannot exceed 100 characters", nameof(newFirstname));

        var firstname = string.IsNullOrWhiteSpace(newFirstname) ? null : newFirstname.Trim();
        return new User(Id, newUsername, newEmail, PasswordHash, Role, CreatedAt, IsActive, firstname, FailedLoginAttempts, LockedUntilUtc, DeletedAt);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}