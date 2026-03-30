using Arc.Application.Telemetry;


namespace Arc.Api.DTOs.Audit
{
    public record AuditLogEntryDto(
        string ExecutionId,
        long Sequence,
        DateTime TimestampUtc,
        AuditEventType EventType,
        string? TaskId,
        string? Message
    )
    {
        public static AuditLogEntryDto FromDomain(AuditLogEntry entry)
            => new(
                entry.ExecutionId,
                entry.Sequence,
                entry.TimestampUtc,
                entry.EventType,
                entry.TaskId,
                entry.Message
            );
    }
}