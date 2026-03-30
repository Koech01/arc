using Arc.Domain.Models;
namespace Arc.Application.Notifications;


/// <summary>
/// Notification service interface for creating and managing notifications.
/// This service abstracts notification creation logic and provides a clean API
/// for triggering notifications from various parts of the application.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a notification for a specific user.
    /// </summary>
    Task CreateNotificationAsync(
        UserId userId,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an execution started notification.
    /// </summary>
    Task NotifyExecutionStartedAsync(
        UserId userId,
        string executionId,
        int taskCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an execution completed notification.
    /// </summary>
    Task NotifyExecutionCompletedAsync(
        UserId userId,
        string executionId,
        int taskCount,
        long durationMs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an execution failed notification.
    /// </summary>
    Task NotifyExecutionFailedAsync(
        UserId userId,
        string executionId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an Info notification when a single task inside an execution finishes.
    /// Fired for every task regardless of whether the result was served from cache.
    /// Provides granular per-task progress visibility in the notifications panel.
    /// </summary>
    Task NotifyTaskCompletedAsync(
        UserId userId,
        string executionId,
        string taskId,
        string taskName,
        bool fromCache,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an Error notification when a webhook delivery permanently fails.
    /// Fired only after all retry attempts for a single target URL are exhausted.
    /// Surfaces silent webhook delivery failures that have no other frontend signal.
    /// </summary>
    Task NotifyWebhookDeliveryFailedAsync(
        UserId userId,
        string webhookUrl,
        string eventType,
        int attempts,
        CancellationToken cancellationToken = default);
}