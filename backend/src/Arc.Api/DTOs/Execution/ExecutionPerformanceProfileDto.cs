namespace Arc.Api.DTOs.Execution;


/// <summary>
/// DTO for task performance metrics in execution profiling.
/// </summary>
public sealed record TaskPerformanceMetricsDto(
    string TaskId,
    string TaskName,
    int ExecutionOrder,
    long ExecutionTimeMs,
    long DependencyWaitTimeMs,
    bool IsCriticalPath,
    IReadOnlyList<string> Dependencies
);

/// <summary>
/// DTO for critical path analysis in execution profiling.
/// </summary>
public sealed record CriticalPathAnalysisDto(
    IReadOnlyList<string> CriticalPathTaskIds,
    long TotalCriticalPathTimeMs,
    double CriticalPathPercentage
);

/// <summary>
/// DTO for resource utilization metrics in execution profiling.
/// </summary>
public sealed record ResourceUtilizationMetricsDto(
    long TotalExecutionTimeMs,
    long ParallelizableTimeMs,
    long SequentialTimeMs,
    double ParallelizationEfficiency,
    int MaxConcurrentTasks,
    double AverageTaskExecutionTimeMs
);

/// <summary>
/// DTO for complete execution performance profile response.
/// </summary>
public sealed record ExecutionPerformanceProfileDto(
    string ExecutionId,
    IReadOnlyList<TaskPerformanceMetricsDto> TaskMetrics,
    CriticalPathAnalysisDto CriticalPath,
    ResourceUtilizationMetricsDto ResourceUtilization,
    DateTime ProfileGeneratedAtUtc
);