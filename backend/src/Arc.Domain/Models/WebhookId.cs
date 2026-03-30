namespace Arc.Domain.Models;

/// <summary>
/// Strongly-typed Webhook identifier.
/// </summary>
/// 
/// 
public sealed class WebhookId
{
    public Guid Value { get; }

    public WebhookId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("WebhookId cannot be empty", nameof(value));
        Value = value;
    }

    public static WebhookId Create() => new(Guid.NewGuid());
    public static WebhookId From(Guid value) => new(value);
    public static WebhookId From(string value) => new(Guid.Parse(value));

    public override bool Equals(object? obj) => obj is WebhookId other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static bool operator ==(WebhookId? left, WebhookId? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(WebhookId? left, WebhookId? right) => !(left == right);
}