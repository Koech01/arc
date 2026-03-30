using Arc.Domain.Models;
namespace Arc.Application.Identity;


/// <summary>
/// Defines user data persistence operations.
/// This interface abstracts user storage from infrastructure concerns.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Creates a new user in the repository.
    /// </summary>
    /// <param name="user">User to create</param>
    /// <returns>The created user</returns>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByIdAsync(UserId userId);

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Updates an existing user in the repository.
    /// </summary>
    /// <param name="user">User to update</param>
    /// <returns>The updated user</returns>
    Task<User> UpdateAsync(User user);

    /// <summary>
    /// Checks if a user with the given email exists.
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <returns>True if user exists, false otherwise</returns>
    Task<bool> ExistsByEmailAsync(string email);

    /// <summary>
    /// Admin-only: returns all users with optional filtering and pagination.
    /// Includes soft-deleted users when includeDeleted is true.
    /// </summary>
    Task<(IReadOnlyList<User> Users, int TotalCount)> GetAllAsync(
        string? emailSearch,
        string? usernameSearch,
        UserRole? role,
        bool? isActive,
        bool includeDeleted,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);
}