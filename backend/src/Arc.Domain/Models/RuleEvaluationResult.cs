namespace Arc.Domain.Models;


/// <summary>
/// Result of evaluating a single divergence rule.
/// Deterministic: same inputs produce same evaluation.
/// </summary>
public sealed class RuleEvaluationResult
{
    public string RuleName { get; }
    public bool Passed { get; }
    public bool DivergenceFound { get; }
    public string? Message { get; }

    // Legacy properties for infrastructure compatibility
    public DivergenceRuleType RuleType { get; }
    public double ActualValue { get; }
    public double Threshold { get; }
    public string Reason { get; }

    public RuleEvaluationResult(string ruleName, bool passed, bool divergenceFound, string? message)
    {
        RuleName = ruleName;
        Passed = passed;
        DivergenceFound = divergenceFound;
        Message = message;
    }

    // Legacy constructor for infrastructure compatibility
    public RuleEvaluationResult(DivergenceRuleType ruleType, bool passed, double actualValue, double threshold, string reason)
    {
        RuleType = ruleType;
        Passed = passed;
        ActualValue = actualValue;
        Threshold = threshold;
        Reason = reason;
    }
}