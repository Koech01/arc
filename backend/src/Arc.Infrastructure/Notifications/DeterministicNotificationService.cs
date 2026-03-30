using Arc.Domain.Models;
using Microsoft.Extensions.Logging;
using Arc.Application.Notifications;
namespace Arc.Infrastructure.Notifications;


/// <summary>
/// Deterministic notification service implementation.
/// Creates notifications with deterministic behavior and structured logging.
/// Fire-and-forget pattern: notification creation failures are logged but don't throw exceptions.
/// </summary>
public sealed class DeterministicNotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<DeterministicNotificationService> _logger;

    public DeterministicNotificationService(
        INotificationRepository repository,
        ILogger<DeterministicNotificationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task CreateNotificationAsync(
        UserId userId,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = Notification.Create(userId, title, message, type);
            await _repository.CreateAsync(notification, cancellationToken);

            _logger.LogInformation(
                "Notification created: {NotificationId} for user {UserId} with type {Type}",
                notification.Id.Value,
                userId.Value,
                type.ToTypeString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create notification for user {UserId}: {Title}",
                userId.Value,
                title);
        }
    }

    public async Task NotifyExecutionStartedAsync(
        UserId userId,
        string executionId,
        int taskCount,
        CancellationToken cancellationToken = default)
    {
        var title = "Execution Started";
        var message = $"Execution {executionId} has started with {taskCount} task(s).";

        await CreateNotificationAsync(userId, title, message, NotificationType.Info, cancellationToken);
    }

    public async Task NotifyExecutionCompletedAsync(
        UserId userId,
        string executionId,
        int taskCount,
        long durationMs,
        CancellationToken cancellationToken = default)
    {
        var title = "Execution Completed";
        var durationSeconds = durationMs / 1000.0;
        var message = $"Execution {executionId} completed successfully. {taskCount} task(s) executed in {durationSeconds:F2}s.";

        await CreateNotificationAsync(userId, title, message, NotificationType.Success, cancellationToken);
    }

    public async Task NotifyExecutionFailedAsync(
        UserId userId,
        string executionId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var title = "Execution Failed";
        var message = $"Execution {executionId} failed: {errorMessage}";

        await CreateNotificationAsync(userId, title, message, NotificationType.Error, cancellationToken);
    }

    /// <summary>
    /// Generates a deterministic Info notification for a completed task within an execution.
    /// The message distinguishes between a freshly executed task and a cache-served result
    /// so users can understand the execution path without inspecting the audit log.
    /// </summary>
    public async Task NotifyTaskCompletedAsync(
        UserId userId,
        string executionId,
        string taskId,
        string taskName,
        bool fromCache,
        CancellationToken cancellationToken = default)
    {
        var title = "Task Completed";
        var source = fromCache ? "served from cache" : "executed";
        var message = $"Task '{taskName}' ({taskId}) {source} in execution {executionId}.";

        await CreateNotificationAsync(userId, title, message, NotificationType.Info, cancellationToken);
    }

    /// <summary>
    /// Generates a deterministic Error notification when a webhook permanently fails delivery.
    /// Only fired after all retry attempts are exhausted - transient failures are never surfaced.
    /// The URL is truncated at 100 characters to stay within the domain's 2000-character message limit.
    /// </summary>
    public async Task NotifyWebhookDeliveryFailedAsync(
        UserId userId,
        string webhookUrl,
        string eventType,
        int attempts,
        CancellationToken cancellationToken = default)
    {
        var title = "Webhook Delivery Failed";
        var truncatedUrl = webhookUrl.Length > 100 ? string.Concat(webhookUrl.AsSpan(0, 100), "...") : webhookUrl;
        var message = $"Webhook delivery for event '{eventType}' failed after {attempts} attempt(s). Target: {truncatedUrl}";

        await CreateNotificationAsync(userId, title, message, NotificationType.Error, cancellationToken);
    }
}