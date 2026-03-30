namespace Arc.Domain.Models;

/// <summary>
/// Webhook event types that can trigger notifications.
/// </summary>
/// 
/// 
/// 
public enum WebhookEventType
{
    ExecutionStarted,
    ExecutionCompleted,
    ExecutionFailed
}

public static class WebhookEventTypeExtensions
{
    public static string ToEventString(this WebhookEventType type) => type switch
    {
        WebhookEventType.ExecutionStarted => "execution.started",
        WebhookEventType.ExecutionCompleted => "execution.completed",
        WebhookEventType.ExecutionFailed => "execution.failed",
        _ => throw new ArgumentException($"Unknown event type: {type}", nameof(type))
    };

    public static WebhookEventType FromEventString(string eventString) => eventString switch
    {
        "execution.started" => WebhookEventType.ExecutionStarted,
        "execution.completed" => WebhookEventType.ExecutionCompleted,
        "execution.failed" => WebhookEventType.ExecutionFailed,
        _ => throw new ArgumentException($"Unknown event string: {eventString}", nameof(eventString))
    };
}