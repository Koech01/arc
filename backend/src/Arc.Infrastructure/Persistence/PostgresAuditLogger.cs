using Arc.Application.Telemetry;
using Arc.Application.Persistence;
namespace Arc.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL implementation of audit logger.
/// </summary>
/// 
/// 
public sealed class PostgresAuditLogger : IAuditLogger
{
    private readonly IDatabaseContext _dbContext;

    public PostgresAuditLogger(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task LogAsync(
        string executionId,
        AuditEventType eventType,
        string? taskId = null,
        string? message = null)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = @"
            INSERT INTO audit_logs (execution_id, sequence_number, event_type, task_id, message, timestamp_utc)
            VALUES (@executionId, 
                    COALESCE((SELECT MAX(sequence_number) FROM audit_logs WHERE execution_id = @executionId), 0) + 1,
                    @eventType, @taskId, @message, @timestamp)";

        AddParameter(command, "@executionId", executionId);
        AddParameter(command, "@eventType", eventType.ToString());
        AddParameter(command, "@taskId", taskId ?? (object)DBNull.Value);
        AddParameter(command, "@message", message ?? string.Empty);
        AddParameter(command, "@timestamp", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();
    }

    public async Task LogImportedAsync(
        string executionId,
        long sequence,
        DateTime timestampUtc,
        AuditEventType eventType,
        string? taskId = null,
        string? message = null)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO audit_logs (execution_id, sequence_number, event_type, task_id, message, timestamp_utc)
            VALUES (@executionId, @sequence, @eventType, @taskId, @message, @timestamp)
            ON CONFLICT (execution_id, sequence_number) DO UPDATE SET
                event_type = EXCLUDED.event_type,
                task_id = EXCLUDED.task_id,
                message = EXCLUDED.message,
                timestamp_utc = EXCLUDED.timestamp_utc";

        AddParameter(command, "@executionId", executionId);
        AddParameter(command, "@sequence", sequence);
        AddParameter(command, "@eventType", eventType.ToString());
        AddParameter(command, "@taskId", taskId ?? (object)DBNull.Value);
        AddParameter(command, "@message", message ?? string.Empty);
        AddParameter(command, "@timestamp", timestampUtc);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetExecutionLogsAsync(
        string executionId)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = @"
            SELECT execution_id, sequence_number, timestamp_utc, event_type, task_id, message
            FROM audit_logs
            WHERE execution_id = @executionId
            ORDER BY sequence_number ASC";

        var param = command.CreateParameter();
        param.ParameterName = "@executionId";
        param.Value = executionId;
        command.Parameters.Add(param);

        var logs = new List<AuditLogEntry>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            logs.Add(new AuditLogEntry(
                reader.GetString(0),
                reader.GetInt64(1),
                reader.GetDateTime(2),
                Enum.Parse<AuditEventType>(reader.GetString(3)),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5)
            ));
        }

        return logs;
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetExecutionLogsAsync(
        string executionId,
        AuditEventType? eventType,
        string? taskId)
    {
        var whereClauses = new List<string> { "execution_id = @executionId" };
        var parameters = new Dictionary<string, object> { ["@executionId"] = executionId };

        if (eventType.HasValue)
        {
            whereClauses.Add("event_type = @eventType");
            parameters["@eventType"] = eventType.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(taskId))
        {
            whereClauses.Add("task_id = @taskId");
            parameters["@taskId"] = taskId;
        }

        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = $@"
            SELECT execution_id, sequence_number, timestamp_utc, event_type, task_id, message
            FROM audit_logs
            WHERE {string.Join(" AND ", whereClauses)}
            ORDER BY sequence_number ASC";

        foreach (var param in parameters)
        {
            AddParameter(command, param.Key, param.Value);
        }

        var logs = new List<AuditLogEntry>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            logs.Add(new AuditLogEntry(
                reader.GetString(0),
                reader.GetInt64(1),
                reader.GetDateTime(2),
                Enum.Parse<AuditEventType>(reader.GetString(3)),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5)
            ));
        }

        return logs;
    }

    private static void AddParameter(System.Data.IDbCommand command, string name, object value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}