using Arc.Domain.Models;
using Arc.Application.Admin;
using Arc.Application.Identity;
using Microsoft.Extensions.Logging;
namespace Arc.Infrastructure.Admin;


/// <summary>
/// Implements admin user lifecycle operations (activate, deactivate, role change,
/// password reset, soft delete) using the existing <see cref="IUserRepository"/> and
/// <see cref="IPasswordHashingService"/> contracts.
/// </summary>
public sealed class AdminUserService : IAdminUserService
{
    private const int MaxPasswordLength = 8;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IAdminAuditLogger _auditLogger;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        IAdminAuditLogger auditLogger,
        ILogger<AdminUserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHashingService = passwordHashingService ?? throw new ArgumentNullException(nameof(passwordHashingService));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AdminUserQueryResult> QueryUsersAsync(
        AdminUserFilter filter,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var (users, total) = await _userRepository.GetAllAsync(
            filter.EmailSearch,
            filter.UsernameSearch,
            filter.Role,
            filter.IsActive,
            filter.IncludeDeleted,
            Math.Clamp(limit, 1, 200),
            Math.Max(0, offset),
            cancellationToken);

        var details = users.Select(MapToDetail).ToList();
        return new AdminUserQueryResult(details, total, limit, offset);
    }

    public async Task<AdminUserDetail?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        return user is null ? null : MapToDetail(user);
    }

    public async Task ActivateUserAsync(Guid userId, Guid actingAdminId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var updated = user.Activate();
        await _userRepository.UpdateAsync(updated);
        _logger.LogInformation("Admin {AdminId} activated user {UserId}", actingAdminId, userId);
        await _auditLogger.LogAsync(new AdminAuditEvent(actingAdminId, AdminAuditAction.ActivatedUser, DateTime.UtcNow, userId.ToString()), cancellationToken);
    }

    public async Task DeactivateUserAsync(Guid userId, Guid actingAdminId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var updated = user.Deactivate();
        await _userRepository.UpdateAsync(updated);
        _logger.LogInformation("Admin {AdminId} deactivated user {UserId}", actingAdminId, userId);
        await _auditLogger.LogAsync(new AdminAuditEvent(actingAdminId, AdminAuditAction.DeactivatedUser, DateTime.UtcNow, userId.ToString()), cancellationToken);
    }

    public async Task ChangeUserRoleAsync(Guid userId, UserRole newRole, Guid actingAdminId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var updated = user.UpdateRole(newRole);
        await _userRepository.UpdateAsync(updated);
        _logger.LogInformation("Admin {AdminId} changed role of user {UserId} to {Role}", actingAdminId, userId, newRole);
        await _auditLogger.LogAsync(new AdminAuditEvent(actingAdminId, AdminAuditAction.ChangedUserRole, DateTime.UtcNow, userId.ToString(), $"NewRole={newRole}"), cancellationToken);
    }

    public async Task ResetUserPasswordAsync(Guid userId, string newPassword, Guid actingAdminId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < MaxPasswordLength)
            throw new ArgumentException($"Password must be at least {MaxPasswordLength} characters", nameof(newPassword));

        var user = await RequireUserAsync(userId, cancellationToken);
        var newHash = _passwordHashingService.HashPassword(newPassword);
        var updated = user.WithNewPasswordHash(newHash).WithResetFailedAttempts();
        await _userRepository.UpdateAsync(updated);
        _logger.LogInformation("Admin {AdminId} reset password for user {UserId}", actingAdminId, userId);
        await _auditLogger.LogAsync(new AdminAuditEvent(actingAdminId, AdminAuditAction.ResetUserPassword, DateTime.UtcNow, userId.ToString()), cancellationToken);
    }

    public async Task DeleteUserAsync(Guid userId, Guid actingAdminId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var deleted = user.SoftDelete();
        await _userRepository.UpdateAsync(deleted);
        _logger.LogWarning("Admin {AdminId} soft-deleted user {UserId}", actingAdminId, userId);
        await _auditLogger.LogAsync(new AdminAuditEvent(actingAdminId, AdminAuditAction.DeletedUser, DateTime.UtcNow, userId.ToString()), cancellationToken);
    }

    // private helpers 

    private async Task<User> RequireUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null)
            throw new InvalidOperationException($"User {userId} not found");
        return user;
    }

    private static AdminUserDetail MapToDetail(User u) => new(
        u.Id.Value,
        u.Username,
        u.Email,
        u.Role.ToString(),
        u.IsDeleted ? "Deleted" : u.IsActive ? "Active" : "Inactive",
        u.CreatedAt,
        u.IsLockedOut,
        u.LockedUntilUtc,
        u.FailedLoginAttempts,
        u.IsDeleted,
        u.DeletedAt,
        u.Firstname
    );
}