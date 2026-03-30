using Npgsql;
using Arc.Application.Admin;
using Arc.Application.Persistence;
using Microsoft.Extensions.Logging;
namespace Arc.Infrastructure.Admin;


/// <summary>
/// PostgreSQL-backed admin audit logger.
/// Writes to an append-only <c>admin_audit_log</c> table.
/// Errors are swallowed and logged so that audit failures never block the primary operation.
/// </summary>
public sealed class PostgresAdminAuditLogger : IAdminAuditLogger
{
    private readonly IDatabaseContext _dbContext;
    private readonly ILogger<PostgresAdminAuditLogger> _logger;

    public PostgresAdminAuditLogger(IDatabaseContext dbContext, ILogger<PostgresAdminAuditLogger> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LogAsync(AdminAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = await _dbContext.OpenConnectionAsync(cancellationToken);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO admin_audit_log
                    (admin_user_id, action, timestamp_utc, target_user_id, detail, ip_address, user_agent)
                VALUES (@admin_user_id, @action, @timestamp_utc, @target_user_id, @detail, @ip_address, @user_agent)",
                conn);

            cmd.Parameters.AddWithValue("admin_user_id", auditEvent.AdminUserId);
            cmd.Parameters.AddWithValue("action", auditEvent.Action.ToString());
            cmd.Parameters.AddWithValue("timestamp_utc", auditEvent.TimestampUtc);
            cmd.Parameters.AddWithValue("target_user_id", (object?)auditEvent.TargetUserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("detail", (object?)auditEvent.Detail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ip_address", (object?)auditEvent.IpAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("user_agent", (object?)auditEvent.UserAgent ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write admin audit log entry. Action={Action} AdminId={AdminId}",
                auditEvent.Action, auditEvent.AdminUserId);
        }
    }

    public async Task<IReadOnlyList<AdminAuditEntry>> GetLogsAsync(
        Guid? adminUserId = null,
        AdminAuditAction? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        if (adminUserId.HasValue)
        {
            conditions.Add("admin_user_id = @admin_user_id");
            parameters.Add(new NpgsqlParameter("admin_user_id", adminUserId.Value));
        }

        if (action.HasValue)
        {
            conditions.Add("action = @action");
            parameters.Add(new NpgsqlParameter("action", action.Value.ToString()));
        }

        if (fromUtc.HasValue)
        {
            conditions.Add("timestamp_utc >= @from_utc");
            parameters.Add(new NpgsqlParameter("from_utc", fromUtc.Value));
        }

        if (toUtc.HasValue)
        {
            conditions.Add("timestamp_utc <= @to_utc");
            parameters.Add(new NpgsqlParameter("to_utc", toUtc.Value));
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        var sql = $@"
            SELECT id, admin_user_id, action, timestamp_utc, target_user_id, detail, ip_address, user_agent
            FROM admin_audit_log
            {where}
            ORDER BY timestamp_utc DESC
            LIMIT @limit OFFSET @offset";

        parameters.Add(new NpgsqlParameter("limit", Math.Clamp(limit, 1, 500)));
        parameters.Add(new NpgsqlParameter("offset", Math.Max(0, offset)));

        await using var conn = await _dbContext.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters.ToArray());

        var entries = new List<AdminAuditEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new AdminAuditEntry(
                reader.GetInt64(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetDateTime(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7)
            ));
        }

        return entries;
    }
}