using Arc.Application.Execution;
using Arc.Application.Telemetry;
namespace Arc.Infrastructure.Execution;


/// <summary>
/// Deterministic execution replayer implementation.
/// Reconstructs execution state and audit trace from persistent storage.
/// No re-execution occurs; results are retrieved from execution result store and audit logs.
/// </summary>
public sealed class DeterministicExecutionReplayer : IExecutionReplayer
{
    private readonly IExecutionResultStore _resultStore;
    private readonly IAuditLogger _auditLogger;

    public DeterministicExecutionReplayer(
        IExecutionResultStore resultStore,
        IAuditLogger auditLogger)
    {
        _resultStore = resultStore ?? throw new ArgumentNullException(nameof(resultStore));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    }

    public async Task<ExecutionReplayResult?> ReplayAsync(string executionId)
    {
        if (string.IsNullOrWhiteSpace(executionId))
        {
            throw new ArgumentException("ExecutionId cannot be null or empty.", nameof(executionId));
        }

        // Retrieve stored execution result
        var result = await _resultStore.GetAsync(executionId);
        if (result is null)
        {
            return null;
        }

        // Retrieve audit trace for the execution
        var auditLogs = await _auditLogger.GetExecutionLogsAsync(executionId);

        // Map audit logs to replay audit entries
        var auditTrace = auditLogs
            .Select(log => new ReplayAuditEntry(
                Sequence: log.Sequence,
                TimestampUtc: log.TimestampUtc,
                EventType: log.EventType.ToString(),
                TaskId: log.TaskId,
                Message: log.Message
            ))
            .ToArray();

        return new ExecutionReplayResult(
            ExecutionId: executionId,
            Tasks: result.Tasks,
            AuditTrace: auditTrace
        );
    }
}
