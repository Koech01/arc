using Arc.Application.Results;
namespace Arc.Application.Execution;


/// <summary>
/// Deterministic batch executor.
/// Processes multiple execution inputs in a single request with aggregated metrics.
/// </summary>
public interface IBatchExecutor
{
    /// <summary>
    /// Executes multiple task graphs deterministically in batch.
    /// </summary>
    /// <param name="inputs">Collection of execution inputs.</param>
    /// <returns>Batch result with all execution outcomes and performance metrics.</returns>
    Task<BatchExecutionResult> ExecuteBatchAsync(IReadOnlyCollection<string> inputs);
}

/// <summary>
/// Individual execution item within a batch.
/// </summary>
public sealed record BatchExecutionItem(
    int Index,
    string ExecutionId,
    IReadOnlyCollection<TaskExecutionResult> Tasks,
    long ExecutionTimeMs,
    string Status
);

/// <summary>
/// Aggregated batch execution result with cross-execution metrics.
/// </summary>
public sealed record BatchExecutionResult(
    string BatchId,
    DateTime CreatedAtUtc,
    IReadOnlyList<BatchExecutionItem> Executions,
    long TotalExecutionTimeMs,
    long AverageExecutionTimeMs,
    int SuccessCount,
    int FailureCount
);
