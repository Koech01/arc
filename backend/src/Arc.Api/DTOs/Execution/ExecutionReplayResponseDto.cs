namespace Arc.Api.DTOs.Execution;


/// <summary>
/// Audit log entry DTO for execution replay trace.
/// </summary>
public sealed record ReplayAuditEntryDto(
    long Sequence,
    DateTime TimestampUtc,
    string EventType,
    string? TaskId,
    string? Message
);

/// <summary>
/// Response DTO for deterministic execution replay.
/// Includes original task results and complete audit trace.
/// </summary>
public sealed record ExecutionReplayResponseDto(
    string ExecutionId,
    IReadOnlyCollection<TaskResultDto> Tasks,
    IReadOnlyList<ReplayAuditEntryDto> AuditTrace
);
