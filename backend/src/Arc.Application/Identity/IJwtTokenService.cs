using Arc.Domain.Models;
namespace Arc.Application.Identity;


/// <summary>
/// Defines JWT token operations for authentication.
/// This interface abstracts token generation and validation from infrastructure concerns.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for the authenticated user.
    /// </summary>
    /// <param name="user">The authenticated user</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(User user);

    /// <summary>
    /// Validates a JWT token and extracts user information.
    /// </summary>
    /// <param name="token">JWT token string</param>
    /// <returns>User ID if token is valid, null otherwise</returns>
    UserId? ValidateToken(string token);

    /// <summary>
    /// Extracts user ID from a JWT token without full validation.
    /// Used for performance-critical scenarios where token validity is assumed.
    /// </summary>
    /// <param name="token">JWT token string</param>
    /// <returns>User ID if extractable, null otherwise</returns>
    UserId? ExtractUserId(string token);
}