using Arc.Application.Results;
namespace Arc.Application.Execution;


/// <summary>
/// Deterministic execution replayer.
/// Given an ExecutionId, reconstructs and simulates execution without re-executing tasks.
/// Uses stored execution results and audit logs for deterministic replay.
/// </summary>
public interface IExecutionReplayer
{
    /// <summary>
    /// Replays a deterministic execution given its execution ID.
    /// Returns the original execution result with audit trace if available.
    /// </summary>
    /// <param name="executionId">The deterministic execution ID to replay.</param>
    /// <returns>Execution result if found; null if not found.</returns>
    Task<ExecutionReplayResult?> ReplayAsync(string executionId);
}

/// <summary>
/// Result of a deterministic execution replay, including audit trace.
/// </summary>
public sealed record ExecutionReplayResult(
    string ExecutionId,
    IReadOnlyCollection<TaskExecutionResult> Tasks,
    IReadOnlyList<ReplayAuditEntry> AuditTrace
);

/// <summary>
/// Audit log entry for replay trace.
/// </summary>
public sealed record ReplayAuditEntry(
    long Sequence,
    DateTime TimestampUtc,
    string EventType,
    string? TaskId,
    string? Message
);
