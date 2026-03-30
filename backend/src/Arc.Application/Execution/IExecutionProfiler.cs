namespace Arc.Application.Execution;


/// <summary>
/// Task performance metrics for profiling analysis.
/// </summary>
public sealed record TaskPerformanceMetrics(
    string TaskId,
    string TaskName,
    int ExecutionOrder,
    long ExecutionTimeMs,
    long DependencyWaitTimeMs,
    bool IsCriticalPath,
    IReadOnlyList<string> Dependencies
);

/// <summary>
/// Critical path analysis for execution workflow.
/// </summary>
public sealed record CriticalPathAnalysis(
    IReadOnlyList<string> CriticalPathTaskIds,
    long TotalCriticalPathTimeMs,
    double CriticalPathPercentage
);

/// <summary>
/// Resource utilization patterns during execution.
/// </summary>
public sealed record ResourceUtilizationMetrics(
    long TotalExecutionTimeMs,
    long ParallelizableTimeMs,
    long SequentialTimeMs,
    double ParallelizationEfficiency,
    int MaxConcurrentTasks,
    double AverageTaskExecutionTimeMs
);

/// <summary>
/// Complete execution performance profile.
/// </summary>
public sealed record ExecutionPerformanceProfile(
    string ExecutionId,
    IReadOnlyList<TaskPerformanceMetrics> TaskMetrics,
    CriticalPathAnalysis CriticalPath,
    ResourceUtilizationMetrics ResourceUtilization,
    DateTime ProfileGeneratedAtUtc
);

/// <summary>
/// Provides deterministic performance profiling for execution workflows.
/// Analyzes task-level execution times, dependency wait times, critical path,
/// and resource utilization patterns for performance optimization.
/// </summary>
public interface IExecutionProfiler
{
    /// <summary>
    /// Generates a deterministic performance profile for the specified execution.
    /// Returns null if the execution is not found.
    /// </summary>
    /// <param name="executionId">The execution ID to profile.</param>
    /// <returns>Complete performance profile or null if execution not found.</returns>
    Task<ExecutionPerformanceProfile?> GenerateProfileAsync(string executionId);
}