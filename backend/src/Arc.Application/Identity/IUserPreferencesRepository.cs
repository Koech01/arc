using Arc.Domain.Models;
namespace Arc.Application.Identity;

/// <summary>
/// Defines user preferences persistence operations.
/// This interface abstracts preferences storage from infrastructure concerns.
/// </summary>
public interface IUserPreferencesRepository
{
    /// <summary>
    /// Gets user preferences by user ID.
    /// </summary>
    Task<UserPreferences?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates user preferences.
    /// </summary>
    Task<UserPreferences> UpsertAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
}