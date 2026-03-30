namespace Arc.Domain.Models;


/// <summary>
/// Strongly-typed identifier for regression gates.
/// </summary>
public readonly record struct RegressionGateId
{
    public Guid Value { get; init; }

    public RegressionGateId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("RegressionGateId cannot be empty.", nameof(value));
        
        Value = value;
    }

    public static RegressionGateId NewId() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(RegressionGateId id) => id.Value;
    public static explicit operator RegressionGateId(Guid value) => new(value);
}