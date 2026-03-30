using Arc.Domain.Models;
namespace Arc.Application.Admin;


/// <summary>
/// Filter parameters for admin user queries.
/// All fields are optional; null means no filter on that axis.
/// </summary>
public sealed record AdminUserFilter(
    string? EmailSearch = null,
    string? UsernameSearch = null,
    UserRole? Role = null,
    bool? IsActive = null,
    bool IncludeDeleted = false
);

/// <summary>
/// Paginated result from an admin user query.
/// </summary>
public sealed record AdminUserQueryResult(
    IReadOnlyList<AdminUserDetail> Users,
    int TotalCount,
    int Limit,
    int Offset
);

/// <summary>
/// Full user detail returned to the admin layer.
/// </summary>
public sealed record AdminUserDetail(
    Guid Id,
    string Username,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    bool IsLockedOut,
    DateTime? LockedUntilUtc,
    int FailedLoginAttempts,
    bool IsDeleted,
    DateTime? DeletedAt,
    string? Firstname
);

/// <summary>
/// Admin service for user lifecycle management operations.
/// All operations require the acting admin's UserId for audit logging.
/// </summary>
public interface IAdminUserService
{
    /// <summary>Queries all users with optional filtering and pagination.</summary>
    Task<AdminUserQueryResult> QueryUsersAsync(
        AdminUserFilter filter,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a single user by their ID.</summary>
    Task<AdminUserDetail?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Activates a user account.</summary>
    Task ActivateUserAsync(Guid userId, Guid actingAdminId, CancellationToken cancellationToken = default);

    /// <summary>Deactivates a user account (non-destructive).</summary>
    Task DeactivateUserAsync(Guid userId, Guid actingAdminId, CancellationToken cancellationToken = default);

    /// <summary>Changes a user's role.</summary>
    Task ChangeUserRoleAsync(Guid userId, UserRole newRole, Guid actingAdminId, CancellationToken cancellationToken = default);

    /// <summary>Sets a new password for a user (admin-initiated reset).</summary>
    Task ResetUserPasswordAsync(Guid userId, string newPassword, Guid actingAdminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a user account. The user record is preserved for audit purposes
    /// but the account becomes permanently inaccessible.
    /// </summary>
    Task DeleteUserAsync(Guid userId, Guid actingAdminId, CancellationToken cancellationToken = default);
}