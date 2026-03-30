namespace Arc.Application.Execution;


/// <summary>
/// Single task comparison item showing differences between two executions.
/// </summary>
public sealed record TaskComparisonItem(
    string TaskId,
    int ExecutionIndex,
    string Status1,
    string Status2,
    string Output1,
    string Output2,
    int ExecutionOrder1,
    int ExecutionOrder2,
    bool IsDifferent
);

/// <summary>
/// Deterministic diff metrics for execution comparison.
/// </summary>
public sealed record ExecutionDiffMetrics(
    int TaskCount,
    int IdenticalTasks,
    int DifferentTasks,
    int DivergencePointIndex,
    bool SameTaskCount,
    bool SameExecutionOrder,
    double SimilarityPercentage
);

/// <summary>
/// Complete execution comparison result.
/// </summary>
public sealed record ExecutionComparisonResult(
    string ExecutionId1,
    string ExecutionId2,
    IReadOnlyList<TaskComparisonItem> TaskComparisons,
    ExecutionDiffMetrics Metrics,
    string Summary
);

/// <summary>
/// Execution comparer for deterministic comparison and diff analysis.
/// </summary>
public interface IExecutionComparer
{
    /// <summary>
    /// Compares two executions deterministically.
    /// Returns detailed task-by-task differences and aggregated metrics.
    /// </summary>
    /// <param name="executionId1">First execution ID.</param>
    /// <param name="executionId2">Second execution ID.</param>
    /// <returns>Comparison result with diff details and metrics; null if either execution not found.</returns>
    Task<ExecutionComparisonResult?> CompareAsync(string executionId1, string executionId2);
}
