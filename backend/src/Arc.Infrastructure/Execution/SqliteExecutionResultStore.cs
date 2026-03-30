using System.Text.Json;
using Microsoft.Data.Sqlite;
using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Infrastructure.Persistence;
namespace Arc.Infrastructure.Execution;


public sealed class SqliteExecutionResultStore : IExecutionResultStore, IDisposable
{
    private readonly SqliteConnection _connection;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new UserIdJsonConverter(), new ExecutionResultJsonConverter() }
    };

    public SqliteExecutionResultStore(string dbPath = "./data/execution_results.db")
    {
        var folder = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(folder))
            Directory.CreateDirectory(folder);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        EnsureTable();
    }

    private void EnsureTable()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ExecutionResults (
                ExecutionId TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                GraphJson TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                WorkflowName TEXT NOT NULL DEFAULT '',
                WorkflowDescription TEXT NOT NULL DEFAULT '',
                IsArchived INTEGER NOT NULL DEFAULT 0
            );";
        cmd.ExecuteNonQuery();

        // Idempotent column additions for existing databases
        try
        {
            using var altCmd1 = _connection.CreateCommand();
            altCmd1.CommandText = "ALTER TABLE ExecutionResults ADD COLUMN WorkflowName TEXT NOT NULL DEFAULT ''";
            altCmd1.ExecuteNonQuery();
        }
        catch (SqliteException) { /* column already exists */ }

        try
        {
            using var altCmd2 = _connection.CreateCommand();
            altCmd2.CommandText = "ALTER TABLE ExecutionResults ADD COLUMN WorkflowDescription TEXT NOT NULL DEFAULT ''";
            altCmd2.ExecuteNonQuery();
        }
        catch (SqliteException) { /* column already exists */ }

        try
        {
            using var altCmd3 = _connection.CreateCommand();
            altCmd3.CommandText = "ALTER TABLE ExecutionResults ADD COLUMN IsArchived INTEGER NOT NULL DEFAULT 0";
            altCmd3.ExecuteNonQuery();
        }
        catch (SqliteException) { /* column already exists */ }
    }

    // Convenience overloads - mirror the interface default implementations so callers
    // using the concrete type (e.g. unit tests) also get the short-hand signatures. 

    public Task StoreAsync(string executionId, ExecutionResult result)
        => StoreAsync(executionId, result, DateTime.UtcNow, null);

    public Task StoreAsync(string executionId, ExecutionResult result, DateTime createdAtUtc)
        => StoreAsync(executionId, result, createdAtUtc, null);

    public Task StoreAsync(string executionId, ExecutionResult result, ExecutionWorkflowContext? workflowContext)
        => StoreAsync(executionId, result, DateTime.UtcNow, workflowContext);

    // Canonical store method 

    public async Task StoreAsync(
        string executionId,
        ExecutionResult result,
        DateTime createdAtUtc,
        ExecutionWorkflowContext? workflowContext)
    {
        var graphJson = JsonSerializer.Serialize(result, JsonOptions);
        var createdAt = createdAtUtc.ToString("o");
        var userId = result.UserId.ToString();
        var workflowName = workflowContext?.WorkflowName ?? string.Empty;
        var workflowDescription = workflowContext?.WorkflowDescription ?? string.Empty;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO ExecutionResults (ExecutionId, UserId, GraphJson, CreatedAt, WorkflowName, WorkflowDescription)
            VALUES ($id, $userId, $json, $createdAt, $workflowName, $workflowDescription)
            ON CONFLICT(ExecutionId) DO UPDATE SET
                GraphJson           = EXCLUDED.GraphJson,
                WorkflowName        = CASE WHEN EXCLUDED.WorkflowName != '' THEN EXCLUDED.WorkflowName ELSE ExecutionResults.WorkflowName END,
                WorkflowDescription = CASE WHEN EXCLUDED.WorkflowDescription != '' THEN EXCLUDED.WorkflowDescription ELSE ExecutionResults.WorkflowDescription END,
                IsArchived          = 0;
        ";
        cmd.Parameters.AddWithValue("$id", executionId);
        cmd.Parameters.AddWithValue("$userId", userId);
        cmd.Parameters.AddWithValue("$json", graphJson);
        cmd.Parameters.AddWithValue("$createdAt", createdAt);
        cmd.Parameters.AddWithValue("$workflowName", workflowName);
        cmd.Parameters.AddWithValue("$workflowDescription", workflowDescription);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<ExecutionResult?> GetAsync(string executionId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT GraphJson FROM ExecutionResults WHERE ExecutionId = $id;";
        cmd.Parameters.AddWithValue("$id", executionId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var graphJson = reader.GetString(0);
        return JsonSerializer.Deserialize<ExecutionResult>(graphJson, JsonOptions);
    }

    public async Task<ExecutionWorkflowContext?> GetWorkflowContextAsync(string executionId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT WorkflowName, WorkflowDescription FROM ExecutionResults WHERE ExecutionId = $id;";
        cmd.Parameters.AddWithValue("$id", executionId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new ExecutionWorkflowContext(
            null,
            reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
            reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
    }

    public async Task<ExecutionQueryResult> QueryAsync(ExecutionQueryFilter? filter, PaginationParams pagination, Guid userId)
    {
        var countQuery = BuildCountQuery(filter, userId);
        var totalAvailable = await ExecuteCountQueryAsync(countQuery.Query, countQuery.Parameters);

        var dataQuery = BuildDataQuery(filter, pagination, userId);
        var executions = await ExecuteDataQueryAsync(dataQuery.Query, dataQuery.Parameters);

        var analyticsQuery = BuildAnalyticsQuery(filter, userId);
        var analytics = await ExecuteAnalyticsQueryAsync(analyticsQuery.Query, analyticsQuery.Parameters);

        return new ExecutionQueryResult(
            Executions: executions,
            Analytics: analytics,
            Limit: pagination.Limit,
            Offset: pagination.Offset,
            TotalAvailable: totalAvailable);
    }

    private (string Query, Dictionary<string, object> Parameters) BuildCountQuery(ExecutionQueryFilter? filter, Guid userId)
    {
        var query = "SELECT COUNT(*) FROM ExecutionResults WHERE UserId = $userId";
        var parameters = new Dictionary<string, object> { ["$userId"] = userId.ToString() };

        if (filter?.IncludeArchived != true)
            query += " AND IsArchived = 0";

        if (filter?.StartDateUtc.HasValue == true)
        {
            query += " AND CreatedAt >= $startDate";
            parameters["$startDate"] = filter.StartDateUtc.Value.ToString("o");
        }

        if (filter?.EndDateUtc.HasValue == true)
        {
            query += " AND CreatedAt <= $endDate";
            parameters["$endDate"] = filter.EndDateUtc.Value.ToString("o");
        }

        return (query, parameters);
    }

    private (string Query, Dictionary<string, object> Parameters) BuildDataQuery(ExecutionQueryFilter? filter, PaginationParams pagination, Guid userId)
    {
        var query = "SELECT ExecutionId, UserId, GraphJson, CreatedAt, WorkflowName, WorkflowDescription, IsArchived FROM ExecutionResults WHERE UserId = $userId";
        var parameters = new Dictionary<string, object> { ["$userId"] = userId.ToString() };

        if (filter?.IncludeArchived != true)
            query += " AND IsArchived = 0";

        if (filter?.StartDateUtc.HasValue == true)
        {
            query += " AND CreatedAt >= $startDate";
            parameters["$startDate"] = filter.StartDateUtc.Value.ToString("o");
        }

        if (filter?.EndDateUtc.HasValue == true)
        {
            query += " AND CreatedAt <= $endDate";
            parameters["$endDate"] = filter.EndDateUtc.Value.ToString("o");
        }

        query += " ORDER BY ExecutionId ASC LIMIT $limit OFFSET $offset";
        parameters["$limit"] = pagination.Limit;
        parameters["$offset"] = pagination.Offset;

        return (query, parameters);
    }

    private (string Query, Dictionary<string, object> Parameters) BuildAnalyticsQuery(ExecutionQueryFilter? filter, Guid userId)
    {
        var query = "SELECT GraphJson FROM ExecutionResults WHERE UserId = $userId";
        var parameters = new Dictionary<string, object> { ["$userId"] = userId.ToString() };

        if (filter?.IncludeArchived != true)
            query += " AND IsArchived = 0";

        if (filter?.StartDateUtc.HasValue == true)
        {
            query += " AND CreatedAt >= $startDate";
            parameters["$startDate"] = filter.StartDateUtc.Value.ToString("o");
        }

        if (filter?.EndDateUtc.HasValue == true)
        {
            query += " AND CreatedAt <= $endDate";
            parameters["$endDate"] = filter.EndDateUtc.Value.ToString("o");
        }

        return (query, parameters);
    }

    private async Task<long> ExecuteCountQueryAsync(string query, Dictionary<string, object> parameters)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = query;
        foreach (var param in parameters)
            cmd.Parameters.AddWithValue(param.Key, param.Value);

        var result = await cmd.ExecuteScalarAsync();
        return result is long l ? l : 0;
    }

    private async Task<List<ExecutionMetadata>> ExecuteDataQueryAsync(string query, Dictionary<string, object> parameters)
    {
        var results = new List<ExecutionMetadata>();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = query;
        foreach (var param in parameters)
            cmd.Parameters.AddWithValue(param.Key, param.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var executionId = reader.GetString(0);
            var graphJson = reader.GetString(2);
            var createdAtStr = reader.GetString(3);
            var workflowName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
            var workflowDescription = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
            var isArchived = !reader.IsDBNull(6) && reader.GetInt32(6) != 0;

            var result = JsonSerializer.Deserialize<ExecutionResult>(graphJson, JsonOptions);
            if (result is null)
                continue;

            results.Add(new ExecutionMetadata(
                ExecutionId: executionId,
                CreatedAtUtc: DateTime.Parse(createdAtStr),
                TaskCount: result.Tasks.Count,
                AverageExecutionTimeMs: result.Tasks.Count,
                Status: DetermineExecutionStatus(result),
                WorkflowName: workflowName,
                WorkflowDescription: workflowDescription,
                IsArchived: isArchived));
        }

        return results;
    }

    private async Task<ExecutionAnalytics> ExecuteAnalyticsQueryAsync(string query, Dictionary<string, object> parameters)
    {
        var executions = new List<ExecutionResult>();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = query;
        foreach (var param in parameters)
            cmd.Parameters.AddWithValue(param.Key, param.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var graphJson = reader.GetString(0);
            var result = JsonSerializer.Deserialize<ExecutionResult>(graphJson, JsonOptions);
            if (result is not null)
                executions.Add(result);
        }

        return CalculateAnalytics(executions);
    }

    private static string DetermineExecutionStatus(ExecutionResult result)
    {
        return result.Tasks.All(t => t.Status == TaskExecutionStatus.Succeeded)
            ? "Succeeded"
            : "PartiallyFailed";
    }

    private static ExecutionAnalytics CalculateAnalytics(List<ExecutionResult> executions)
    {
        var totalCount = executions.Count;
        var successCount = executions.Count(e => e.Tasks.All(t => t.Status == TaskExecutionStatus.Succeeded));
        var failureCount = totalCount - successCount;
        var successRate = totalCount > 0 ? successCount / (double)totalCount : 0;
        var averageTaskCount = totalCount > 0 ? executions.Sum(e => e.Tasks.Count) / totalCount : 0;
        var averageExecutionTime = totalCount > 0 ? executions.Sum(e => e.Tasks.Count) / totalCount : 0;

        return new ExecutionAnalytics(
            TotalCount: totalCount,
            SuccessCount: successCount,
            FailureCount: failureCount,
            SuccessRate: successRate,
            AverageTaskCount: averageTaskCount,
            AverageExecutionTimeMs: averageExecutionTime);
    }

    public void Dispose() => _connection?.Dispose();

    public Task ArchiveAsync(string executionId, Guid archivedBy, string? reason = null, int? retentionDays = null)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE ExecutionResults SET IsArchived = 1 WHERE ExecutionId = $id";
        cmd.Parameters.AddWithValue("$id", executionId);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task UnarchiveAsync(string executionId, Guid unarchivedBy)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE ExecutionResults SET IsArchived = 0 WHERE ExecutionId = $id";
        cmd.Parameters.AddWithValue("$id", executionId);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task PurgeAsync(string executionId, Guid purgedBy, string? reason = null)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM ExecutionResults WHERE ExecutionId = $id";
        cmd.Parameters.AddWithValue("$id", executionId);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ArchiveAuditEntry>> GetArchiveAuditAsync(string executionId)
        => Task.FromResult<IReadOnlyList<ArchiveAuditEntry>>(Array.Empty<ArchiveAuditEntry>());
}