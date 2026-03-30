namespace Arc.Domain.Models;

/// <summary>
/// Strongly-typed identifier for golden executions.
/// Uses the same format as ExecutionId (SHA256 hash string).
/// </summary>
public readonly record struct GoldenExecutionId
{
    public string Value { get; init; }

    public GoldenExecutionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("GoldenExecutionId cannot be null or empty.", nameof(value));
        
        Value = value.Trim();
    }

    public override string ToString() => Value;

    public static implicit operator string(GoldenExecutionId id) => id.Value;
    public static explicit operator GoldenExecutionId(string value) => new(value);
}