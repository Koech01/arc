namespace Arc.Domain.Models;


/// <summary>
/// Notification type enumeration.
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    ExecutionCompleted,
    ExecutionFailed,
    WebhookDeliveryFailed,
    SystemUpdate
}

/// <summary>
/// Extension methods for NotificationType.
/// </summary>
public static class NotificationTypeExtensions
{
    public static string ToTypeString(this NotificationType type)
    {
        return type switch
        {
            NotificationType.Info => "info",
            NotificationType.Success => "success",
            NotificationType.Warning => "warning",
            NotificationType.Error => "error",
            NotificationType.ExecutionCompleted => "execution.completed",
            NotificationType.ExecutionFailed => "execution.failed",
            NotificationType.WebhookDeliveryFailed => "webhook.delivery.failed",
            NotificationType.SystemUpdate => "system.update",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown notification type")
        };
    }

    public static NotificationType FromTypeString(string typeString)
    {
        return typeString?.ToLowerInvariant() switch
        {
            "info" => NotificationType.Info,
            "success" => NotificationType.Success,
            "warning" => NotificationType.Warning,
            "error" => NotificationType.Error,
            "execution.completed" => NotificationType.ExecutionCompleted,
            "execution.failed" => NotificationType.ExecutionFailed,
            "webhook.delivery.failed" => NotificationType.WebhookDeliveryFailed,
            "system.update" => NotificationType.SystemUpdate,
            _ => throw new ArgumentException($"Unknown notification type: {typeString}", nameof(typeString))
        };
    }
}