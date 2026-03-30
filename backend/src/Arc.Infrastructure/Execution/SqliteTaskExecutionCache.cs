using System.Text.Json;
using Microsoft.Data.Sqlite;
using Arc.Application.Results;
using Arc.Application.Execution;
namespace Arc.Infrastructure.Execution;


public sealed class SqliteTaskExecutionCache : ITaskExecutionCache, IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTaskExecutionCache(string dbPath = "./data/task_cache.db")
    {
        var folder = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(folder))
            Directory.CreateDirectory(folder);

        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        _connection = new SqliteConnection(cs);
        _connection.Open();
        EnsureTable();
    }

    private void EnsureTable()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS TaskCache (
                TaskHash TEXT PRIMARY KEY,
                ResultJson TEXT NOT NULL,
                ExpiresAtUtc TEXT NOT NULL
            );
        """;
        cmd.ExecuteNonQuery();
    }

    public async Task<TaskExecutionResult?> GetAsync(string taskHash)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT ResultJson, ExpiresAtUtc
            FROM TaskCache
            WHERE TaskHash = $hash;
        """;
        cmd.Parameters.AddWithValue("$hash", taskHash);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var expiresAt = DateTime.Parse(reader.GetString(1));
        if (DateTime.UtcNow > expiresAt)
        {
            await InvalidateAsync(taskHash);
            return null;
        }

        return JsonSerializer.Deserialize<TaskExecutionResult>(reader.GetString(0));
    }

    public async Task StoreAsync(string taskHash, TaskExecutionResult result, DateTime expiresAtUtc)
    {
        var json = JsonSerializer.Serialize(result);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO TaskCache (TaskHash, ResultJson, ExpiresAtUtc)
            VALUES ($hash, $json, $exp);
        """;
        cmd.Parameters.AddWithValue("$hash", taskHash);
        cmd.Parameters.AddWithValue("$json", json);
        cmd.Parameters.AddWithValue("$exp", expiresAtUtc.ToString("o"));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task InvalidateAsync(string? taskHash = null)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = taskHash is null
            ? "DELETE FROM TaskCache;"
            : "DELETE FROM TaskCache WHERE TaskHash = $hash;";
        if (taskHash is not null)
            cmd.Parameters.AddWithValue("$hash", taskHash);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<CacheStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                COUNT(*)                                          AS total,
                SUM(CASE WHEN ExpiresAtUtc < $now THEN 1 ELSE 0 END) AS expired,
                MIN(ExpiresAtUtc)                                 AS oldest,
                MAX(ExpiresAtUtc)                                 AS newest
            FROM TaskCache;
        """;
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new CacheStats(0, 0, 0, null, null);

        var total = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
        var expired = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
        DateTime? oldest = reader.IsDBNull(2) ? null : DateTime.Parse(reader.GetString(2));
        DateTime? newest = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3));

        return new CacheStats(total, expired, total - expired, oldest, newest);
    }

    public void Dispose() => _connection.Dispose();
}