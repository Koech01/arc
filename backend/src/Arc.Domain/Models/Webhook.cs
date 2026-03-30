using Arc.Domain.Exceptions;
namespace Arc.Domain.Models;


/// <summary>
/// Webhook domain model representing a registered webhook for execution events.
/// Webhooks are user-owned and receive deterministic HMAC-SHA256 signed payloads.
/// </summary>
public sealed class Webhook
{
    public WebhookId Id { get; }
    public string Url { get; }
    public IReadOnlyList<WebhookEventType> Events { get; }
    public string Secret { get; }
    public bool IsActive { get; }
    public UserId CreatedBy { get; }
    public DateTime CreatedAt { get; }

    public Webhook(
        WebhookId id,
        string url,
        IReadOnlyList<WebhookEventType> events,
        string secret,
        bool isActive,
        UserId createdBy,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new WebhookException("Webhook URL cannot be empty");
        
        if (!IsValidUrl(url))
            throw new WebhookException($"Invalid webhook URL: {url}");
        
        if (events == null || events.Count == 0)
            throw new WebhookException("Webhook must subscribe to at least one event");
        
        if (string.IsNullOrWhiteSpace(secret))
            throw new WebhookException("Webhook secret cannot be empty");
        
        if (secret.Length < 20)
            throw new WebhookException("Webhook secret must be at least 20 characters");
        
        if (createdBy.Value == Guid.Empty)
            throw new WebhookException("Webhook must have a creator");

        Id = id;
        Url = url.Trim();
        Events = events;
        Secret = secret;
        IsActive = isActive;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Returns true if the webhook is subscribed to the given event type.
    /// </summary>
    public bool IsSubscribedTo(WebhookEventType eventType) => Events.Contains(eventType);

    private static bool IsValidUrl(string url)
    {
        try
        {
            var uri = new Uri(url, UriKind.Absolute);
            return uri.Scheme is "http" or "https";
        }
        catch
        {
            return false;
        }
    }
}