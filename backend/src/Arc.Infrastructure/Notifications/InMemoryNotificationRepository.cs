using Arc.Domain.Models;
using System.Collections.Concurrent;
using Arc.Application.Notifications;
namespace Arc.Infrastructure.Notifications;


/// <summary>
/// In-memory notification repository for lightweight tests and local helpers.
/// </summary>
public sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly ConcurrentDictionary<Guid, Notification> _store = new();

    public Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _store[notification.Id.Value] = notification;
        return Task.FromResult(notification);
    }

    public Task<Notification?> GetByIdAsync(NotificationId notificationId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(notificationId.Value, out var notification);
        return Task.FromResult(notification);
    }

    public Task<IReadOnlyList<Notification>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var notifications = _store.Values
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<Notification>>(notifications);
    }

    public Task<Notification> UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _store[notification.Id.Value] = notification;
        return Task.FromResult(notification);
    }

    public Task<int> MarkAllAsReadAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var key in _store.Keys.ToList())
        {
            if (_store.TryGetValue(key, out var n) && n.UserId == userId && !n.IsRead)
            {
                _store[key] = new Notification(n.Id, n.UserId, n.Title, n.Message, n.Type, true, n.CreatedAt);
                count++;
            }
        }
        return Task.FromResult(count);
    }

    public Task DeleteAsync(NotificationId notificationId, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(notificationId.Value, out _);
        return Task.CompletedTask;
    }
}
