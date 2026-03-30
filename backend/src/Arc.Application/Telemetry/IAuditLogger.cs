namespace Arc.Application.Telemetry
{
    public enum AuditEventType
    {
        OrchestratorStarted,
        TaskStarted,
        TaskFinished,
        OrchestratorFinished
    }

    public record AuditLogEntry(
        string ExecutionId,
        long Sequence,
        DateTime TimestampUtc,
        AuditEventType EventType,
        string? TaskId,
        string? Message
    );

    public interface IAuditLogger
    {
        Task LogAsync(
            string executionId,
            AuditEventType eventType,
            string? taskId = null,
            string? message = null
        );

        /// <summary>
        /// Logs an imported audit entry with an explicit sequence and timestamp.
        /// Used by deterministic import paths to preserve original ordering and time.
        /// </summary>
        Task LogImportedAsync(
            string executionId,
            long sequence,
            DateTime timestampUtc,
            AuditEventType eventType,
            string? taskId = null,
            string? message = null
        );

        Task<IReadOnlyList<AuditLogEntry>> GetExecutionLogsAsync(
            string executionId
        );

        /// <summary>
        /// Retrieves deterministic audit logs for an execution with optional filters.
        /// Filtering never affects ordering.
        /// </summary>
        Task<IReadOnlyList<AuditLogEntry>> GetExecutionLogsAsync(
            string executionId,
            AuditEventType? eventType,
            string? taskId
        );
    }
}