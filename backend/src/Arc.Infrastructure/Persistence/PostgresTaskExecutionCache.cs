using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.Persistence;

namespace Arc.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL implementation of task execution cache.
/// </summary>
public sealed class PostgresTaskExecutionCache : ITaskExecutionCache
{
    private readonly IDatabaseContext _dbContext;

    public PostgresTaskExecutionCache(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<TaskExecutionResult?> GetAsync(string taskHash)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = @"
            SELECT task_id, task_name, output, status
            FROM task_execution_cache
            WHERE task_hash = @taskHash AND expires_at_utc > @now";

        AddParameter(command, "@taskHash", taskHash);
        AddParameter(command, "@now", DateTime.UtcNow);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new TaskExecutionResult(
            reader.GetString(0),
            reader.GetString(1),
            0,
            Enum.Parse<TaskExecutionStatus>(reader.GetString(3)),
            reader.GetString(2)
        );
    }

    public async Task StoreAsync(
        string taskHash,
        TaskExecutionResult result,
        DateTime expiresAtUtc)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = @"
            INSERT INTO task_execution_cache (task_hash, task_id, task_name, output, status, cached_at_utc, expires_at_utc)
            VALUES (@taskHash, @taskId, @taskName, @output, @status, @cachedAt, @expiresAt)
            ON CONFLICT (task_hash) DO UPDATE SET
                task_id = EXCLUDED.task_id,
                task_name = EXCLUDED.task_name,
                output = EXCLUDED.output,
                status = EXCLUDED.status,
                cached_at_utc = EXCLUDED.cached_at_utc,
                expires_at_utc = EXCLUDED.expires_at_utc";

        AddParameter(command, "@taskHash", taskHash);
        AddParameter(command, "@taskId", result.TaskId);
        AddParameter(command, "@taskName", result.TaskName);
        AddParameter(command, "@output", result.Output);
        AddParameter(command, "@status", result.Status.ToString());
        AddParameter(command, "@cachedAt", DateTime.UtcNow);
        AddParameter(command, "@expiresAt", expiresAtUtc);

        await command.ExecuteNonQueryAsync();
    }

    public async Task InvalidateAsync(string? taskHash = null)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        if (string.IsNullOrWhiteSpace(taskHash))
        {
            command.CommandText = "DELETE FROM task_execution_cache";
        }
        else
        {
            command.CommandText = "DELETE FROM task_execution_cache WHERE task_hash = @taskHash";
            var param = command.CreateParameter();
            param.ParameterName = "@taskHash";
            param.Value = taskHash;
            command.Parameters.Add(param);
        }

        await command.ExecuteNonQueryAsync();
    }

    public async Task<CacheStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                COUNT(*) AS total,
                SUM(CASE WHEN expires_at_utc <= @now THEN 1 ELSE 0 END) AS expired,
                SUM(CASE WHEN expires_at_utc > @now THEN 1 ELSE 0 END) AS active,
                MIN(cached_at_utc) AS oldest,
                MAX(cached_at_utc) AS newest
            FROM task_execution_cache";

        AddParameter(command, "@now", DateTime.UtcNow);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new CacheStats(0, 0, 0, null, null);

        var total = reader.IsDBNull(0) ? 0 : (int)reader.GetInt64(0);
        var expired = reader.IsDBNull(1) ? 0 : (int)reader.GetInt64(1);
        var active = reader.IsDBNull(2) ? 0 : (int)reader.GetInt64(2);
        var oldest = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
        var newest = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);

        return new CacheStats(total, expired, active, oldest, newest);
    }

    private static void AddParameter(System.Data.IDbCommand command, string name, object value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}