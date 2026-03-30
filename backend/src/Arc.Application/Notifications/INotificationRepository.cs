using Arc.Domain.Models;
namespace Arc.Application.Notifications;


/// <summary>
/// Defines notification persistence operations.
/// This interface abstracts notification storage from infrastructure concerns.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Creates a new notification.
    /// </summary>
    Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a notification by ID.
    /// </summary>
    Task<Notification?> GetByIdAsync(NotificationId notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all notifications for a specific user, ordered by creation date descending.
    /// </summary>
    Task<IReadOnlyList<Notification>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing notification.
    /// </summary>
    Task<Notification> UpdateAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications as read for a specific user.
    /// </summary>
    Task<int> MarkAllAsReadAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a notification by ID.
    /// </summary>
    Task DeleteAsync(NotificationId notificationId, CancellationToken cancellationToken = default);
}