using Npgsql;
using Arc.Domain.Models;
using Arc.Application.Admin;
using Arc.Application.Persistence;
using Microsoft.Extensions.Logging;
namespace Arc.Infrastructure.Admin;


/// <summary>
/// PostgreSQL-backed login history repository.
/// Records every login attempt. Errors are swallowed so they never block the auth flow.
/// </summary>
public sealed class PostgresLoginHistoryRepository : ILoginHistoryRepository
{
    private readonly IDatabaseContext _dbContext;
    private readonly ILogger<PostgresLoginHistoryRepository> _logger;

    public PostgresLoginHistoryRepository(IDatabaseContext dbContext, ILogger<PostgresLoginHistoryRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RecordAsync(
        UserId userId,
        bool success,
        string? failureReason = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = await _dbContext.OpenConnectionAsync(cancellationToken);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO login_history (user_id, timestamp_utc, success, failure_reason, ip_address, user_agent)
                VALUES (@user_id, @timestamp_utc, @success, @failure_reason, @ip_address, @user_agent)",
                conn);

            cmd.Parameters.AddWithValue("user_id", userId.Value);
            cmd.Parameters.AddWithValue("timestamp_utc", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("success", success);
            cmd.Parameters.AddWithValue("failure_reason", (object?)failureReason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ip_address", (object?)ipAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("user_agent", (object?)userAgent ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record login history for user {UserId}", userId.Value);
        }
    }

    public async Task<IReadOnlyList<LoginHistoryEntry>> GetByUserIdAsync(
        UserId userId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _dbContext.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(@"
            SELECT id, user_id, timestamp_utc, success, failure_reason, ip_address, user_agent
            FROM login_history
            WHERE user_id = @user_id
            ORDER BY timestamp_utc DESC
            LIMIT @limit",
            conn);

        cmd.Parameters.AddWithValue("user_id", userId.Value);
        cmd.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 200));

        var entries = new List<LoginHistoryEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new LoginHistoryEntry(
                reader.GetInt64(0),
                new UserId(reader.GetGuid(1)),
                reader.GetDateTime(2),
                reader.GetBoolean(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6)
            ));
        }

        return entries;
    }
}