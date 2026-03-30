namespace Arc.Api.DTOs.Execution;


/// <summary>
/// Task comparison item DTO showing differences between two executions.
/// </summary>
public sealed record TaskComparisonItemDto(
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
/// Diff metrics DTO for execution comparison.
/// </summary>
public sealed record ExecutionDiffMetricsDto(
    int TaskCount,
    int IdenticalTasks,
    int DifferentTasks,
    int DivergencePointIndex,
    bool SameTaskCount,
    bool SameExecutionOrder,
    double SimilarityPercentage
);

/// <summary>
/// Execution comparison response DTO.
/// </summary>
public sealed record ExecutionComparisonResponseDto(
    string ExecutionId1,
    string ExecutionId2,
    IReadOnlyList<TaskComparisonItemDto> TaskComparisons,
    ExecutionDiffMetricsDto Metrics,
    string Summary
);

/// <summary>
/// Request DTO for execution comparison.
/// </summary>
public sealed record ExecutionCompareRequestDto(
    string ExecutionId1,
    string ExecutionId2
);
