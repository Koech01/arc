namespace Arc.Domain.Models;


/// <summary>
/// Represents a user identity using a GUID.
/// This is a value object that wraps a stable, persistent GUID for user identification.
/// </summary>
public readonly record struct UserId(Guid Value)
{
    /// <summary>
    /// Creates a UserId from a GUID.
    /// </summary>
    public static UserId From(Guid guid) => new(guid);

    /// <summary>
    /// Creates a UserId from a string representation of a GUID.
    /// </summary>
    public static UserId From(string guidString)
    {
        if (!Guid.TryParse(guidString, out var guid))
            throw new ArgumentException($"Invalid GUID format: {guidString}", nameof(guidString));
        
        return new UserId(guid);
    }

    /// <summary>
    /// Returns the string representation of the underlying GUID.
    /// </summary>
    public override string ToString() => Value.ToString();

    /// <summary>
    /// Default anonymous user for fallback scenarios.
    /// This is a fixed GUID representing anonymous/default user.
    /// </summary>
    public static readonly UserId Anonymous = new(new Guid("00000000-0000-0000-0000-000000000001"));
}