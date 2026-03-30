namespace Arc.Application.Execution;


/// <summary>
/// Task node for dependency graph visualization.
/// </summary>
public sealed record TaskGraphNode(
    string TaskId,
    string TaskName,
    int ExecutionOrder,
    string Status,
    IReadOnlyList<string> Dependencies,
    bool IsCriticalPath,
    long ExecutionTimeMs
);

/// <summary>
/// Timeline event for execution visualization.
/// </summary>
public sealed record TimelineEvent(
    string TaskId,
    string TaskName,
    DateTime StartTime,
    DateTime EndTime,
    long DurationMs,
    string EventType,
    bool IsCriticalPath
);

/// <summary>
/// Resource allocation snapshot at a point in time.
/// </summary>
public sealed record ResourceSnapshot(
    DateTime Timestamp,
    int ActiveTasks,
    IReadOnlyList<string> RunningTaskIds
);

/// <summary>
/// Complete execution workflow visualization data.
/// </summary>
public sealed record ExecutionVisualization(
    string ExecutionId,
    IReadOnlyList<TaskGraphNode> DependencyGraph,
    IReadOnlyList<TimelineEvent> ExecutionTimeline,
    IReadOnlyList<string> CriticalPathTaskIds,
    IReadOnlyList<ResourceSnapshot> ResourceAllocation,
    DateTime GeneratedAtUtc
);

/// <summary>
/// Provides deterministic visualization data for execution workflows.
/// Generates structured data for task dependency graphs, execution timelines,
/// critical path highlighting, and resource allocation over time.
/// </summary>
public interface IExecutionVisualizer
{
    /// <summary>
    /// Generates deterministic visualization data for the specified execution.
    /// Returns null if the execution is not found.
    /// </summary>
    /// <param name="executionId">The execution ID to visualize.</param>
    /// <returns>Complete visualization data or null if execution not found.</returns>
    Task<ExecutionVisualization?> GenerateVisualizationAsync(string executionId);
}