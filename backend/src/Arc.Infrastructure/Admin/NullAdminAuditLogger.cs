using Arc.Application.Admin;
namespace Arc.Infrastructure.Admin;


/// <summary>
/// No-op admin audit logger for environments without a PostgreSQL backend.
/// Admin actions are still logged via Serilog structured logging; this prevents
/// audit writes from failing the primary operation when the DB is unavailable.
/// </summary>
public sealed class NullAdminAuditLogger : IAdminAuditLogger
{
    public Task LogAsync(AdminAuditEvent auditEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<AdminAuditEntry>> GetLogsAsync(
        Guid? adminUserId = null,
        AdminAuditAction? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AdminAuditEntry>>(Array.Empty<AdminAuditEntry>());
}