namespace Arc.Api.DTOs.Execution;


/// <summary>
/// Individual execution item in batch response.
/// </summary>
public sealed record BatchExecutionResponseItem(
    int Index,
    string ExecutionId,
    IReadOnlyCollection<TaskResultDto> Tasks,
    long ExecutionTimeMs,
    string Status
);

/// <summary>
/// Batch execution response DTO with aggregated metrics.
/// </summary>
public sealed record BatchExecuteResponseDto(
    string BatchId,
    DateTime CreatedAtUtc,
    IReadOnlyList<BatchExecutionResponseItem> Executions,
    long TotalExecutionTimeMs,
    long AverageExecutionTimeMs,
    int SuccessCount,
    int FailureCount
);
