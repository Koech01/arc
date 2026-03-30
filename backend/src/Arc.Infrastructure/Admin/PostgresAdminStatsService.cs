using Npgsql;
using Arc.Application.Admin;
using Arc.Application.Persistence;
using Microsoft.Extensions.Logging;
namespace Arc.Infrastructure.Admin;
using Microsoft.Extensions.Caching.Memory;


public sealed class PostgresAdminStatsService : IAdminStatsService
{
    private static readonly TimeSpan StatsCacheTtl = TimeSpan.FromSeconds(30);
    private const string StatsCacheKey = "admin:stats";

    private readonly IDatabaseContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PostgresAdminStatsService> _logger;

    public PostgresAdminStatsService(
        IDatabaseContext dbContext,
        IMemoryCache cache,
        ILogger<PostgresAdminStatsService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AdminStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(StatsCacheKey, out AdminStats? cached) && cached is not null)
            return cached;

        await using var conn = await _dbContext.OpenConnectionAsync(cancellationToken);

        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var today = DateTime.UtcNow.Date;

        var totalUsers = await GetCountAsync(conn, "SELECT COUNT(*) FROM users", cancellationToken);
        var activeUsers = await GetCountAsync(conn, "SELECT COUNT(*) FROM users WHERE is_active = true AND deleted_at IS NULL", cancellationToken);
        var newUsersThisWeek = await GetCountAsync(conn, "SELECT COUNT(*) FROM users WHERE created_at_utc >= @weekAgo AND deleted_at IS NULL", cancellationToken, ("weekAgo", weekAgo));
        var activeLLMs = await GetCountAsync(conn, "SELECT COUNT(*) FROM llm_configurations WHERE is_active = true", cancellationToken);
        var newLLMsThisWeek = await GetCountAsync(conn, "SELECT COUNT(*) FROM llm_configurations WHERE created_at >= @weekAgo", cancellationToken, ("weekAgo", weekAgo));
        var totalExecutions = await GetCountAsync(conn, "SELECT COUNT(*) FROM execution_results", cancellationToken);
        var executionsToday = await GetCountAsync(conn, "SELECT COUNT(*) FROM execution_results WHERE created_at_utc >= @today", cancellationToken, ("today", today));

        var stats = new AdminStats(totalUsers, activeUsers, newUsersThisWeek, activeLLMs, newLLMsThisWeek, totalExecutions, executionsToday);

        _cache.Set(StatsCacheKey, stats, StatsCacheTtl);
        return stats;
    }

    public async Task<IReadOnlyList<UserInfo>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = await _dbContext.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            "SELECT id, username, email, role, is_active, created_at_utc FROM users WHERE deleted_at IS NULL ORDER BY created_at_utc DESC",
            conn);

        var users = new List<UserInfo>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new UserInfo(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetBoolean(4) ? "Active" : "Inactive",
                reader.GetDateTime(5)
            ));
        }

        return users;
    }

    public async Task<AdminExecutionOverview> GetSystemExecutionsAsync(
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();
        var paramValues = new List<(string Name, object Value)>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("er.status = @status");
            paramValues.Add(("status", status));
        }
        if (fromUtc.HasValue)
        {
            conditions.Add("er.created_at_utc >= @from");
            paramValues.Add(("from", fromUtc.Value));
        }
        if (toUtc.HasValue)
        {
            conditions.Add("er.created_at_utc <= @to");
            paramValues.Add(("to", toUtc.Value));
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        await using var conn = await _dbContext.OpenConnectionAsync(cancellationToken);

        await using var countCmd = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM execution_results er {where}", conn);
        foreach (var (name, value) in paramValues)
            countCmd.Parameters.AddWithValue(name, value);
        var total = (int)Convert.ToInt64(await countCmd.ExecuteScalarAsync(cancellationToken));

        await using var dataCmd = new NpgsqlCommand($@"
            SELECT er.execution_id, er.user_id, u.email, er.status, er.created_at_utc,
                   er.task_count, er.execution_time_ms, er.workflow_name
            FROM execution_results er
            LEFT JOIN users u ON u.id = er.user_id
            {where}
            ORDER BY er.created_at_utc DESC
            LIMIT @limit OFFSET @offset", conn);
        foreach (var (name, value) in paramValues)
            dataCmd.Parameters.AddWithValue(name, value);
        dataCmd.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 200));
        dataCmd.Parameters.AddWithValue("offset", Math.Max(0, offset));

        var rows = new List<AdminExecutionRow>();
        await using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AdminExecutionRow(
                reader.GetString(0),
                reader.GetGuid(1),
                reader.IsDBNull(2) ? "unknown" : reader.GetString(2),
                reader.GetString(3),
                reader.GetDateTime(4),
                reader.GetInt32(5),
                reader.GetInt64(6),
                reader.IsDBNull(7) ? null : reader.GetString(7)
            ));
        }

        return new AdminExecutionOverview(rows, total, limit, offset);
    }

    public async Task<IReadOnlyList<AdminLLMConfigRow>> GetAllLLMConfigsAsync(
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _dbContext.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(@"
            SELECT lc.id, lc.name, lc.model, lc.base_url, lc.is_active, lc.created_at, u.email
            FROM llm_configurations lc
            LEFT JOIN users u ON u.id = lc.created_by
            ORDER BY lc.created_at DESC
            LIMIT @limit OFFSET @offset", conn);
        cmd.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 200));
        cmd.Parameters.AddWithValue("offset", Math.Max(0, offset));

        var rows = new List<AdminLLMConfigRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AdminLLMConfigRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetBoolean(4),
                reader.GetDateTime(5),
                reader.IsDBNull(6) ? "unknown" : reader.GetString(6)
            ));
        }

        return rows;
    }

    private static async Task<int> GetCountAsync(
        NpgsqlConnection conn,
        string sql,
        CancellationToken cancellationToken,
        params (string name, object value)[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long count ? (int)count : 0;
    }
}