namespace Arc.Domain.Models;


/// <summary>
/// Represents a user notification in the system.
/// Notifications are immutable once created, except for the read status.
/// </summary>
public sealed class Notification
{
    public NotificationId Id { get; }
    public UserId UserId { get; }
    public string Title { get; }
    public string Message { get; }
    public NotificationType Type { get; }
    public bool IsRead { get; }
    public DateTime CreatedAt { get; }

    public Notification(
        NotificationId id,
        UserId userId,
        string title,
        string message,
        NotificationType type,
        bool isRead,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or empty", nameof(title));

        if (title.Length > 255)
            throw new ArgumentException("Title cannot exceed 255 characters", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty", nameof(message));

        if (message.Length > 2000)
            throw new ArgumentException("Message cannot exceed 2000 characters", nameof(message));

        Id = id ?? throw new ArgumentNullException(nameof(id));
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        IsRead = isRead;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new notification with default values.
    /// </summary>
    public static Notification Create(
        UserId userId,
        string title,
        string message,
        NotificationType type)
    {
        return new Notification(
            NotificationId.Create(),
            userId,
            title,
            message,
            type,
            isRead: false,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Marks the notification as read.
    /// </summary>
    public Notification MarkAsRead()
    {
        if (IsRead)
            return this;

        return new Notification(Id, UserId, Title, Message, Type, isRead: true, CreatedAt);
    }
}