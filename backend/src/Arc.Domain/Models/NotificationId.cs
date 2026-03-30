namespace Arc.Domain.Models;


/// <summary>
/// Strongly-typed Notification identifier.
/// </summary>
public sealed class NotificationId
{
    public Guid Value { get; }

    public NotificationId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("NotificationId cannot be empty", nameof(value));
        Value = value;
    }

    public static NotificationId Create() => new(Guid.NewGuid());
    public static NotificationId From(Guid value) => new(value);
    public static NotificationId From(string value) => new(Guid.Parse(value));

    public override bool Equals(object? obj) => obj is NotificationId other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static bool operator ==(NotificationId? left, NotificationId? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(NotificationId? left, NotificationId? right) => !(left == right);
}