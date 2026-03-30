using Npgsql;
using Arc.Domain.Models;
using Arc.Application.Persistence;
using Arc.Application.RegressionGates;
namespace Arc.Infrastructure.RegressionGates;


/// <summary>
/// PostgreSQL-backed golden execution store.
/// Stores metadata about executions marked as golden baselines.
/// </summary>
public sealed class PostgresGoldenExecutionStore : IGoldenExecutionStore
{
    private readonly IDatabaseContext _dbContext;

    public PostgresGoldenExecutionStore(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task MarkAsGoldenAsync(string executionId, string? label, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            throw new ArgumentException("Execution ID cannot be null or empty", nameof(executionId));

        const string sql = @"
            INSERT INTO golden_executions (execution_id, owner_id, label, marked_at_utc)
            SELECT @executionId, user_id, @label, @markedAtUtc
            FROM execution_results
            WHERE execution_id = @executionId
            ON CONFLICT (execution_id) DO UPDATE
            SET label = EXCLUDED.label, marked_at_utc = EXCLUDED.marked_at_utc";

        await using var connection = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("executionId", executionId);
        command.Parameters.AddWithValue("label", (object?)label ?? DBNull.Value);
        command.Parameters.AddWithValue("markedAtUtc", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> IsGoldenAsync(string executionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            throw new ArgumentException("Execution ID cannot be null or empty", nameof(executionId));

        const string sql = "SELECT COUNT(*) FROM golden_executions WHERE execution_id = @executionId";

        await using var connection = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("executionId", executionId);

        var count = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        return count > 0;
    }

    public async Task<IReadOnlyList<GoldenExecutionMetadata>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT execution_id, owner_id, label, marked_at_utc
            FROM golden_executions
            WHERE owner_id = @ownerId
            ORDER BY marked_at_utc DESC";

        await using var connection = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("ownerId", userId.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        var results = new List<GoldenExecutionMetadata>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var executionId = reader.GetString(0);
            var ownerId = new UserId(reader.GetGuid(1));
            var label = reader.IsDBNull(2) ? null : reader.GetString(2);
            var markedAtUtc = reader.GetDateTime(3);

            results.Add(new GoldenExecutionMetadata(executionId, ownerId, label, markedAtUtc));
        }

        return results;
    }

    public async Task<bool> UnmarkAsGoldenAsync(string executionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            throw new ArgumentException("Execution ID cannot be null or empty", nameof(executionId));

        const string sql = "DELETE FROM golden_executions WHERE execution_id = @executionId";

        await using var connection = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("executionId", executionId);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }
}