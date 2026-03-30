using Arc.Domain.Models;
namespace Arc.Application.Webhooks;


/// <summary>
/// Represents a webhook event containing execution context information.
/// Sent to registered webhooks when execution events occur.
/// </summary>
public sealed class WebhookEventPayload
{
    public string ExecutionId { get; }
    public WebhookEventType EventType { get; }
    public DateTime Timestamp { get; }
    public UserId UserId { get; }
    public int TaskCount { get; }
    public string Status { get; } // "success", "failed", "running"
    public long DurationMs { get; }
    public string? ErrorMessage { get; }

    public WebhookEventPayload(
        string executionId,
        WebhookEventType eventType,
        DateTime timestamp,
        UserId userId,
        int taskCount,
        string status,
        long durationMs,
        string? errorMessage = null)
    {
        ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
        EventType = eventType;
        Timestamp = timestamp;
        UserId = userId;
        TaskCount = taskCount;
        Status = status ?? throw new ArgumentNullException(nameof(status));
        DurationMs = durationMs;
        ErrorMessage = errorMessage;
    }
}