using Arc.Domain.Exceptions;
namespace Arc.Domain.Models;


/// <summary>
/// Rule for determining pass/fail in regression gate evaluation.
/// Immutable value object with validation.
/// </summary>
public sealed class DivergenceRule
{
    public DivergenceRuleType Type { get; init; }
    public double Threshold { get; init; }

    public DivergenceRule(DivergenceRuleType type, double threshold)
    {
        if (threshold < 0.0 || threshold > 1.0)
            throw new DivergenceRuleInvalidException($"Threshold must be between 0.0 and 1.0, got {threshold}");

        Type = type;
        Threshold = threshold;
    }

    public override string ToString() => $"{Type.ToStringValue()} >= {Threshold:P0}";
}

public sealed class DivergenceRuleInvalidException : DomainException
{
    public DivergenceRuleInvalidException(string message) : base(message) { }
}