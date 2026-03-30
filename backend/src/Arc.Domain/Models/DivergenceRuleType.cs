namespace Arc.Domain.Models;

/// <summary>
/// Types of divergence rules for regression gate evaluation.
/// </summary>
public enum DivergenceRuleType
{
    /// <summary>
    /// Overall similarity percentage must be >= threshold (0.0 - 1.0).
    /// </summary>
    SimilarityPercentage,

    /// <summary>
    /// No individual task can differ by more than threshold (0.0 - 1.0).
    /// </summary>
    MaxTaskDivergence,

    /// <summary>
    /// Critical path tasks must be identical (threshold ignored, treated as 1.0).
    /// </summary>
    CriticalPathPreservation,

    /// <summary>
    /// No task can change from Succeeded to Failed (threshold ignored, treated as 1.0).
    /// </summary>
    NoStatusDegradation
}

public static class DivergenceRuleTypeExtensions
{
    /// <summary>
    /// Converts DivergenceRuleType to deterministic string representation.
    /// </summary>
    public static string ToStringValue(this DivergenceRuleType type)
    {
        return type switch
        {
            DivergenceRuleType.SimilarityPercentage => "similarity_percentage",
            DivergenceRuleType.MaxTaskDivergence => "max_task_divergence",
            DivergenceRuleType.CriticalPathPreservation => "critical_path_preservation",
            DivergenceRuleType.NoStatusDegradation => "no_status_degradation",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown DivergenceRuleType")
        };
    }

    /// <summary>
    /// Parses deterministic string representation to DivergenceRuleType.
    /// </summary>
    public static DivergenceRuleType FromStringValue(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "similarity_percentage" => DivergenceRuleType.SimilarityPercentage,
            "max_task_divergence" => DivergenceRuleType.MaxTaskDivergence,
            "critical_path_preservation" => DivergenceRuleType.CriticalPathPreservation,
            "no_status_degradation" => DivergenceRuleType.NoStatusDegradation,
            _ => throw new ArgumentException($"Unknown divergence rule type: {value}", nameof(value))
        };
    }
}