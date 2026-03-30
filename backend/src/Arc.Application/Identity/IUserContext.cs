using Arc.Domain.Models;
namespace Arc.Application.Identity;


/// <summary>
/// Provides access to the current user context.
/// This abstraction allows the Application layer to access user identity
/// without depending on infrastructure concerns like HTTP headers.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the current user's identity.
    /// This should return a stable, persistent UserId for the current request context.
    /// </summary>
    UserId CurrentUserId { get; }
}