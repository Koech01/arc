using Arc.Domain.Models;
namespace Arc.Application.Identity;


/// <summary>
/// Defines authentication operations for the application.
/// This interface abstracts authentication logic from infrastructure concerns.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new user with the provided credentials.
    /// </summary>
    /// <param name="username">User's username</param>
    /// <param name="email">User's email address</param>
    /// <param name="password">Plain text password</param>
    /// <param name="role">User role</param>
    /// <returns>The created user</returns>
    /// <exception cref="Domain.Exceptions.AuthenticationException">Thrown when registration fails</exception>
    Task<User> RegisterAsync(string username, string email, string password, UserRole role = UserRole.User);

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">Plain text password</param>
    /// <returns>The authenticated user</returns>
    /// <exception cref="Domain.Exceptions.AuthenticationException">Thrown when authentication fails</exception>
    Task<User> AuthenticateAsync(string email, string password);

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetUserByIdAsync(UserId userId);

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Updates a user's profile information.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="newUsername">New username</param>
    /// <param name="newEmail">New email address</param>
    /// <param name="newFirstname">New firstname (optional)</param>
    /// <returns>The updated user</returns>
    /// <exception cref="Domain.Exceptions.AuthenticationException">Thrown when update fails</exception>
    Task<User> UpdateProfileAsync(UserId userId, string newUsername, string newEmail, string? newFirstname = null);
}