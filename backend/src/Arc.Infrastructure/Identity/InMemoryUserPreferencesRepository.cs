using Arc.Domain.Models;
using System.Collections.Concurrent;
using Arc.Application.Identity;
namespace Arc.Infrastructure.Identity;


/// <summary>
/// In-memory user preferences repository for SQLite/development fallback.
/// </summary>
public sealed class InMemoryUserPreferencesRepository : IUserPreferencesRepository
{
    private readonly ConcurrentDictionary<Guid, UserPreferences> _store = new();

    public Task<UserPreferences?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(userId.Value, out var prefs);
        return Task.FromResult(prefs);
    }

    public Task<UserPreferences> UpsertAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        _store[preferences.UserId.Value] = preferences;
        return Task.FromResult(preferences);
    }
}
