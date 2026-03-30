namespace Arc.Api.DTOs.Execution;


/// <summary>
/// Single execution metadata item for list response (analytics/paginated format).
/// </summary>
public sealed record ExecutionListItemDto(
    string ExecutionId,
    DateTime CreatedAtUtc,
    int TaskCount,
    long AverageExecutionTimeMs,
    string Status,
    string WorkflowName,
    string WorkflowDescription,
    bool IsArchived
);

/// <summary>
/// Aggregated analytics for filtered executions.
/// </summary>
public sealed record ExecutionAnalyticsDto(
    long TotalCount,
    long SuccessCount,
    long FailureCount,
    double SuccessRate,
    long AverageTaskCount,
    long AverageExecutionTimeMs
);

/// <summary>
/// Paginated list response for execution queries.
/// </summary>
public sealed record ExecutionListResponseDto(
    IReadOnlyList<ExecutionListItemDto> Executions,
    ExecutionAnalyticsDto Analytics,
    int Limit,
    int Offset,
    long TotalAvailable,
    long TotalPages
);