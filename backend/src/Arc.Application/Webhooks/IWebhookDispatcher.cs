namespace Arc.Application.Webhooks;

/// <summary>
/// Dispatches webhook events to registered webhooks.
/// Handles HTTP delivery, retries with exponential backoff, and HMAC-SHA256 signing.
/// </summary>
/// 
public interface IWebhookDispatcher
{
    /// <summary>
    /// Dispatches an event to all active webhooks subscribed to the event type.
    /// Does not throw; logs errors instead.
    /// </summary>
    Task DispatchAsync(WebhookEventPayload payload, CancellationToken cancellationToken = default);
}