using System.Text.Json;
using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.Persistence;
namespace Arc.Infrastructure.Persistence;


/// <summary>
/// PostgreSQL implementation of execution result store.
/// </summary>
public sealed class PostgresExecutionResultStore : IExecutionResultStore
{
    private readonly IDatabaseContext _dbContext;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new UserIdJsonConverter(), new ExecutionResultJsonConverter() }
    };

    public PostgresExecutionResultStore(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    // Canonical store - all other overloads delegate here via default interface methods 

    public async Task StoreAsync(
        string executionId,
        ExecutionResult result,
        DateTime createdAtUtc,
        ExecutionWorkflowContext? workflowContext)
    {
        var resultJson = JsonSerializer.Serialize(result, JsonOptions);
        var userId = result.UserId.Value;

        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO execution_results (
                execution_id, user_id, created_at_utc, task_count, status,
                execution_time_ms, result_json, workflow_id, workflow_name, workflow_description)
            VALUES (
                @executionId, @userId, @createdAt, @taskCount, @status,
                @executionTimeMs, @resultJson, @workflowId, @workflowName, @workflowDescription)
            ON CONFLICT (execution_id) DO UPDATE SET
                result_json          = EXCLUDED.result_json,
                workflow_id          = COALESCE(EXCLUDED.workflow_id, execution_results.workflow_id),
                workflow_name        = COALESCE(EXCLUDED.workflow_name, execution_results.workflow_name),
                workflow_description = COALESCE(EXCLUDED.workflow_description, execution_results.workflow_description),
                is_archived          = false";

        AddParameter(command, "@executionId", executionId);
        AddParameter(command, "@userId", userId);
        AddParameter(command, "@createdAt", createdAtUtc);
        AddParameter(command, "@taskCount", result.Tasks.Count);
        AddParameter(command, "@status", DetermineStatus(result));
        AddParameter(command, "@executionTimeMs", result.Tasks.Count);
        AddParameter(command, "@resultJson", resultJson);
        AddParameter(command, "@workflowId", (object?)workflowContext?.WorkflowId ?? DBNull.Value);
        AddParameter(command, "@workflowName", (object?)workflowContext?.WorkflowName ?? DBNull.Value);
        AddParameter(command, "@workflowDescription", (object?)workflowContext?.WorkflowDescription ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<ExecutionResult?> GetAsync(string executionId)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT result_json FROM execution_results WHERE execution_id = @executionId";
        AddParameter(command, "@executionId", executionId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var json = reader.GetString(0);
        return JsonSerializer.Deserialize<ExecutionResult>(json, JsonOptions);
    }

    public async Task<ExecutionWorkflowContext?> GetWorkflowContextAsync(string executionId)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT workflow_id, workflow_name, workflow_description
            FROM execution_results
            WHERE execution_id = @executionId";

        AddParameter(command, "@executionId", executionId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var workflowId = reader.IsDBNull(0) ? null : reader.GetString(0);
        var name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        var description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

        return new ExecutionWorkflowContext(workflowId, name, description);
    }

    public async Task<ExecutionQueryResult> QueryAsync(
        ExecutionQueryFilter? filter,
        PaginationParams pagination,
        Guid userId)
    {
        var whereClauses = new List<string>();
        var parameters = new Dictionary<string, object>();

        whereClauses.Add("user_id = @userId");
        parameters["@userId"] = userId;

        if (filter?.IncludeArchived != true)
            whereClauses.Add("is_archived = false");

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                whereClauses.Add("status = @status");
                parameters["@status"] = filter.Status;
            }

            if (filter.StartDateUtc.HasValue)
            {
                whereClauses.Add("created_at_utc >= @startDate");
                parameters["@startDate"] = filter.StartDateUtc.Value;
            }

            if (filter.EndDateUtc.HasValue)
            {
                whereClauses.Add("created_at_utc <= @endDate");
                parameters["@endDate"] = filter.EndDateUtc.Value;
            }

            if (filter.MinTaskCount.HasValue)
            {
                whereClauses.Add("task_count >= @minTaskCount");
                parameters["@minTaskCount"] = filter.MinTaskCount.Value;
            }

            if (filter.MaxTaskCount.HasValue)
            {
                whereClauses.Add("task_count <= @maxTaskCount");
                parameters["@maxTaskCount"] = filter.MaxTaskCount.Value;
            }
        }

        var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        await using var connection = await _dbContext.OpenConnectionAsync();

        // Total count
        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = $"SELECT COUNT(*) FROM execution_results {whereClause}";
        foreach (var param in parameters)
            AddParameter(countCommand, param.Key, param.Value);
        var totalCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync());

        // Paginated results
        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT execution_id, created_at_utc, task_count, execution_time_ms, status,
                   COALESCE(workflow_name, ''), COALESCE(workflow_description, ''), is_archived
            FROM execution_results
            {whereClause}
            ORDER BY execution_id ASC
            LIMIT @limit OFFSET @offset";

        foreach (var param in parameters)
            AddParameter(command, param.Key, param.Value);
        AddParameter(command, "@limit", pagination.Limit);
        AddParameter(command, "@offset", pagination.Offset);

        var executions = new List<ExecutionMetadata>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            executions.Add(new ExecutionMetadata(
                reader.GetString(0),
                reader.GetDateTime(1),
                reader.GetInt32(2),
                reader.GetInt64(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetBoolean(7)
            ));
        }

        var successCount = executions.Count(e => e.Status == "Succeeded");
        var failureCount = executions.Count - successCount;
        var successRate = totalCount > 0 ? (double)successCount / totalCount : 0;
        var avgTaskCount = executions.Count > 0 ? (long)executions.Average(e => e.TaskCount) : 0;
        var avgExecutionTime = executions.Count > 0 ? (long)executions.Average(e => e.AverageExecutionTimeMs) : 0;

        var analytics = new ExecutionAnalytics(
            totalCount, successCount, failureCount, successRate, avgTaskCount, avgExecutionTime);

        return new ExecutionQueryResult(executions, analytics, pagination.Limit, pagination.Offset, totalCount);
    }

    private static string DetermineStatus(ExecutionResult result)
    {
        return result.Tasks.All(t => t.Status == TaskExecutionStatus.Succeeded)
            ? "Succeeded"
            : "PartiallyFailed";
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }

    public async Task ArchiveAsync(string executionId, Guid archivedBy, string? reason = null, int? retentionDays = null)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await using var archiveCmd = connection.CreateCommand();
            archiveCmd.Transaction = transaction;
            archiveCmd.CommandText = @"
                UPDATE execution_results
                SET is_archived = true,
                    archived_at_utc = @archivedAt,
                    archived_by = @archivedBy,
                    archive_reason = @reason,
                    retention_expires_at_utc = @retentionExpires
                WHERE execution_id = @executionId";

            AddParameter(archiveCmd, "@executionId", executionId);
            AddParameter(archiveCmd, "@archivedAt", DateTime.UtcNow);
            AddParameter(archiveCmd, "@archivedBy", archivedBy);
            AddParameter(archiveCmd, "@reason", (object?)reason ?? DBNull.Value);
            AddParameter(archiveCmd, "@retentionExpires",
                retentionDays.HasValue ? DateTime.UtcNow.AddDays(retentionDays.Value) : DBNull.Value);

            await archiveCmd.ExecuteNonQueryAsync();
            await LogArchiveAuditAsync(connection, transaction, executionId, "ARCHIVE", archivedBy, reason);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UnarchiveAsync(string executionId, Guid unarchivedBy)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await using var unarchiveCmd = connection.CreateCommand();
            unarchiveCmd.Transaction = transaction;
            unarchiveCmd.CommandText = @"
                UPDATE execution_results
                SET is_archived = false,
                    archived_at_utc = NULL,
                    archived_by = NULL,
                    archive_reason = NULL,
                    retention_expires_at_utc = NULL
                WHERE execution_id = @executionId";

            AddParameter(unarchiveCmd, "@executionId", executionId);
            await unarchiveCmd.ExecuteNonQueryAsync();
            await LogArchiveAuditAsync(connection, transaction, executionId, "UNARCHIVE", unarchivedBy, null);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task PurgeAsync(string executionId, Guid purgedBy, string? reason = null)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await LogArchiveAuditAsync(connection, transaction, executionId, "PURGE", purgedBy, reason);
            await using var deleteCmd = connection.CreateCommand();
            deleteCmd.Transaction = transaction;
            deleteCmd.CommandText = "DELETE FROM execution_results WHERE execution_id = @executionId";
            AddParameter(deleteCmd, "@executionId", executionId);
            await deleteCmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<ArchiveAuditEntry>> GetArchiveAuditAsync(string executionId)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT id, execution_id, action, performed_by, performed_at_utc, reason, ip_address, user_agent
            FROM execution_archive_audit
            WHERE execution_id = @executionId
            ORDER BY performed_at_utc DESC";

        AddParameter(command, "@executionId", executionId);

        var entries = new List<ArchiveAuditEntry>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new ArchiveAuditEntry(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetGuid(3),
                reader.GetDateTime(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7)
            ));
        }

        return entries;
    }

    private static async Task LogArchiveAuditAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        string executionId,
        string action,
        Guid performedBy,
        string? reason)
    {
        await using var auditCmd = connection.CreateCommand();
        auditCmd.Transaction = transaction;
        auditCmd.CommandText = @"
            INSERT INTO execution_archive_audit
            (execution_id, action, performed_by, performed_at_utc, reason)
            VALUES (@executionId, @action, @performedBy, @performedAt, @reason)";

        AddParameter(auditCmd, "@executionId", executionId);
        AddParameter(auditCmd, "@action", action);
        AddParameter(auditCmd, "@performedBy", performedBy);
        AddParameter(auditCmd, "@performedAt", DateTime.UtcNow);
        AddParameter(auditCmd, "@reason", (object?)reason ?? DBNull.Value);

        await auditCmd.ExecuteNonQueryAsync();
    }
}