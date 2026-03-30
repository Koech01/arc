namespace Arc.Api.DTOs.Execution;


/// <summary>
/// DTO for task graph node in visualization.
/// </summary>
public sealed record TaskGraphNodeDto(
    string TaskId,
    string TaskName,
    int ExecutionOrder,
    string Status,
    IReadOnlyList<string> Dependencies,
    bool IsCriticalPath,
    long ExecutionTimeMs
);

/// <summary>
/// DTO for timeline event in visualization.
/// </summary>
public sealed record TimelineEventDto(
    string TaskId,
    string TaskName,
    DateTime StartTime,
    DateTime EndTime,
    long DurationMs,
    string EventType,
    bool IsCriticalPath
);

/// <summary>
/// DTO for resource allocation snapshot.
/// </summary>
public sealed record ResourceSnapshotDto(
    DateTime Timestamp,
    int ActiveTasks,
    IReadOnlyList<string> RunningTaskIds
);

/// <summary>
/// DTO for complete execution workflow visualization response.
/// </summary>
public sealed record ExecutionVisualizationDto(
    string ExecutionId,
    IReadOnlyList<TaskGraphNodeDto> DependencyGraph,
    IReadOnlyList<TimelineEventDto> ExecutionTimeline,
    IReadOnlyList<string> CriticalPathTaskIds,
    IReadOnlyList<ResourceSnapshotDto> ResourceAllocation,
    DateTime GeneratedAtUtc
);