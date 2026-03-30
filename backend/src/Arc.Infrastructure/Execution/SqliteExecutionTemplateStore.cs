using System.Text.Json;
using Arc.Domain.Models;
using Microsoft.Data.Sqlite;
using Arc.Application.Execution;
using System.Text.RegularExpressions;
namespace Arc.Infrastructure.Execution;


/// <summary>
/// SQLite-backed persistent storage for execution templates.
/// All operations are scoped to the owning user (multi-tenant).
/// For :memory: databases a single shared connection is kept open so the schema persists.
/// </summary>
public sealed class SqliteExecutionTemplateStore : IExecutionTemplateStore
{
    private readonly string _connectionString;
    private readonly SqliteConnection? _sharedConnection;

    public SqliteExecutionTemplateStore(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

        if (connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            _sharedConnection = new SqliteConnection(connectionString);
            _sharedConnection.Open();
        }

        InitializeDatabase();
    }

    private SqliteConnection OpenConnection()
    {
        if (_sharedConnection != null)
            return _sharedConnection;

        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void CloseConnection(SqliteConnection conn)
    {
        if (_sharedConnection == null)
            conn.Dispose();
    }

    private void InitializeDatabase()
    {
        var conn = OpenConnection();
        try
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ExecutionTemplates (
                    Id TEXT PRIMARY KEY NOT NULL,
                    Name TEXT NOT NULL,
                    UserId TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    TasksJson TEXT NOT NULL,
                    TriggerType TEXT NOT NULL,
                    CreatedAtUtc TEXT NOT NULL,
                    UseCount INTEGER NOT NULL DEFAULT 0,
                    UNIQUE(Name, UserId)
                );
                CREATE INDEX IF NOT EXISTS idx_ExecutionTemplates_UserId ON ExecutionTemplates(UserId);
                CREATE INDEX IF NOT EXISTS idx_ExecutionTemplates_CreatedAtUtc ON ExecutionTemplates(CreatedAtUtc DESC);
            ";
            cmd.ExecuteNonQuery();

            try
            {
                var altCmd = conn.CreateCommand();
                altCmd.CommandText = "ALTER TABLE ExecutionTemplates ADD COLUMN UserId TEXT NOT NULL DEFAULT ''";
                altCmd.ExecuteNonQuery();
            }
            catch (SqliteException) { /* column already exists */ }
        }
        finally
        {
            CloseConnection(conn);
        }
    }

    // ── Convenience overloads (no UserId) ────────────────────────────────────

    public Task<ExecutionTemplate> CreateAsync(string name, string description, IReadOnlyList<WorkflowTask> tasks, string triggerType, string? llmConfigId = null)
        => CreateAsync(name, description, tasks, triggerType, UserId.Anonymous, llmConfigId);

    public Task<ExecutionTemplate?> GetAsync(string name)
        => GetAsync(name, UserId.Anonymous);

    public Task<IReadOnlyList<ExecutionTemplate>> ListAsync()
        => ListAsync(UserId.Anonymous);

    public Task<bool> DeleteAsync(string name)
        => DeleteAsync(name, UserId.Anonymous);

    public Task<TemplateInstantiationResult?> InstantiateAsync(string templateName, Dictionary<string, string>? variables = null)
        => InstantiateAsync(templateName, UserId.Anonymous, variables);

    // ── Interface implementations ─────────────────────────────────────────────

    public async Task<ExecutionTemplate> CreateAsync(string name, string description, IReadOnlyList<WorkflowTask> tasks, string triggerType, UserId userId, string? llmConfigId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be null or empty.", nameof(name));
        if (tasks is null || tasks.Count == 0)
            throw new ArgumentException("Template must have at least one task.", nameof(tasks));
        if (string.IsNullOrWhiteSpace(triggerType))
            throw new ArgumentException("Template trigger type cannot be null or empty.", nameof(triggerType));

        name = name.Trim();

        var conn = OpenConnection();
        try
        {
            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM ExecutionTemplates WHERE Name = @name AND UserId = @userId";
            checkCmd.Parameters.AddWithValue("@name", name);
            checkCmd.Parameters.AddWithValue("@userId", userId.Value.ToString());
            var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0L) > 0;

            if (exists)
                throw new InvalidOperationException($"Template '{name}' already exists.");

            var tasksJson = JsonSerializer.Serialize(tasks.Select(t => new TaskData(
                t.Id, t.Name, t.AgentType, t.Prompt, t.LLMConfigId,
                t.Config.ToDictionary(kv => kv.Key, kv => kv.Value),
                t.Dependencies.ToList())).ToList());

            var createdAtUtc = DateTime.UtcNow.ToString("O");

            var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO ExecutionTemplates (Id, Name, UserId, Description, TasksJson, TriggerType, CreatedAtUtc, UseCount)
                VALUES (@id, @name, @userId, @description, @tasksJson, @triggerType, @createdAtUtc, 0)";
            insertCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString("D"));
            insertCmd.Parameters.AddWithValue("@name", name);
            insertCmd.Parameters.AddWithValue("@userId", userId.Value.ToString());
            insertCmd.Parameters.AddWithValue("@description", description ?? "");
            insertCmd.Parameters.AddWithValue("@tasksJson", tasksJson);
            insertCmd.Parameters.AddWithValue("@triggerType", triggerType);
            insertCmd.Parameters.AddWithValue("@createdAtUtc", createdAtUtc);
            await insertCmd.ExecuteNonQueryAsync();

            return new ExecutionTemplate(name, description ?? "", tasks, triggerType, llmConfigId, DateTime.Parse(createdAtUtc), 0);
        }
        finally
        {
            CloseConnection(conn);
        }
    }

    public async Task<ExecutionTemplate?> GetAsync(string name, UserId userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var conn = OpenConnection();
        try
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, Description, TasksJson, TriggerType, CreatedAtUtc, UseCount
                FROM ExecutionTemplates WHERE Name = @name AND UserId = @userId";
            cmd.Parameters.AddWithValue("@name", name.Trim());
            cmd.Parameters.AddWithValue("@userId", userId.Value.ToString());

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadTemplate(reader) : null;
        }
        finally
        {
            CloseConnection(conn);
        }
    }

    public async Task<IReadOnlyList<ExecutionTemplate>> ListAsync(UserId userId)
    {
        var conn = OpenConnection();
        try
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, Description, TasksJson, TriggerType, CreatedAtUtc, UseCount
                FROM ExecutionTemplates WHERE UserId = @userId ORDER BY Name ASC";
            cmd.Parameters.AddWithValue("@userId", userId.Value.ToString());

            var templates = new List<ExecutionTemplate>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                templates.Add(ReadTemplate(reader));

            return templates.AsReadOnly();
        }
        finally
        {
            CloseConnection(conn);
        }
    }

    public async Task<bool> DeleteAsync(string name, UserId userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var conn = OpenConnection();
        try
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM ExecutionTemplates WHERE Name = @name AND UserId = @userId";
            cmd.Parameters.AddWithValue("@name", name.Trim());
            cmd.Parameters.AddWithValue("@userId", userId.Value.ToString());
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        finally
        {
            CloseConnection(conn);
        }
    }

    public async Task<bool> UpdateAsync(string name, string description, IReadOnlyList<WorkflowTask> tasks, string triggerType, UserId userId, string? llmConfigId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var tasksJson = JsonSerializer.Serialize(tasks.Select(t => new TaskData(
            t.Id, t.Name, t.AgentType, t.Prompt, t.LLMConfigId,
            t.Config.ToDictionary(kv => kv.Key, kv => kv.Value),
            t.Dependencies.ToList())).ToList());

        var conn = OpenConnection();
        try
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE ExecutionTemplates
                SET Description = @description, TasksJson = @tasksJson, TriggerType = @triggerType
                WHERE Name = @name AND UserId = @userId";
            cmd.Parameters.AddWithValue("@name", name.Trim());
            cmd.Parameters.AddWithValue("@userId", userId.Value.ToString());
            cmd.Parameters.AddWithValue("@description", description ?? string.Empty);
            cmd.Parameters.AddWithValue("@tasksJson", tasksJson);
            cmd.Parameters.AddWithValue("@triggerType", triggerType);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        finally
        {
            CloseConnection(conn);
        }
    }

    public async Task<TemplateInstantiationResult?> InstantiateAsync(string templateName, UserId userId, Dictionary<string, string>? variables = null)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return null;

        var conn = OpenConnection();
        try
        {
            var selectCmd = conn.CreateCommand();
            selectCmd.CommandText = @"
                SELECT Id, Name, Description, TasksJson, TriggerType, CreatedAtUtc, UseCount
                FROM ExecutionTemplates WHERE Name = @name AND UserId = @userId";
            selectCmd.Parameters.AddWithValue("@name", templateName.Trim());
            selectCmd.Parameters.AddWithValue("@userId", userId.Value.ToString());

            ExecutionTemplate? template;
            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                    return null;
                template = ReadTemplate(reader);
            }

            var runNumber = template.UseCount + 1;

            var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = "UPDATE ExecutionTemplates SET UseCount = UseCount + 1 WHERE Name = @name AND UserId = @userId";
            updateCmd.Parameters.AddWithValue("@name", templateName.Trim());
            updateCmd.Parameters.AddWithValue("@userId", userId.Value.ToString());
            await updateCmd.ExecuteNonQueryAsync();

            var instantiatedTasks = SubstituteVariables(template.Tasks, variables ?? new());
            return new TemplateInstantiationResult(templateName.Trim(), instantiatedTasks, template.TriggerType, template.LLMConfigId, DateTime.UtcNow, runNumber);
        }
        finally
        {
            CloseConnection(conn);
        }
    }

    private static ExecutionTemplate ReadTemplate(SqliteDataReader reader)
    {
        var name = reader.GetString(1);
        var description = reader.GetString(2);
        var tasksJson = reader.GetString(3);
        var triggerType = reader.GetString(4);
        var createdAtUtcStr = reader.GetString(5);
        var useCount = reader.GetInt32(6);

        var taskDataList = JsonSerializer.Deserialize<List<TaskData>>(tasksJson)
            ?? throw new InvalidOperationException("Failed to deserialize template tasks.");

        var tasks = taskDataList.Select(td => new WorkflowTask(
            td.Id, td.Name, td.AgentType, td.Prompt, td.LLMConfigId,
            td.Config ?? new Dictionary<string, string>(),
            td.Dependencies ?? new List<string>())).ToList();

        return new ExecutionTemplate(name, description, tasks, triggerType, null, DateTime.Parse(createdAtUtcStr), useCount);
    }

    private sealed record TaskData(
        string Id, string Name, string AgentType,
        string? Prompt, string? LLMConfigId,
        Dictionary<string, string>? Config, List<string>? Dependencies);

    private static IReadOnlyList<WorkflowTask> SubstituteVariables(IReadOnlyList<WorkflowTask> tasks, Dictionary<string, string> variables)
    {
        if (variables.Count == 0)
            return tasks;
        return tasks.Select(t => SubstituteTaskVariables(t, variables)).ToList();
    }

    private static WorkflowTask SubstituteTaskVariables(WorkflowTask task, Dictionary<string, string> variables)
    {
        return new WorkflowTask(
            SubstituteString(task.Id, variables),
            SubstituteString(task.Name, variables),
            task.AgentType,
            task.Prompt != null ? SubstituteString(task.Prompt, variables) : null,
            task.LLMConfigId,
            task.Config,
            task.Dependencies);
    }

    private static readonly Regex PlaceholderPattern = new Regex(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled);

    private static string SubstituteString(string input, Dictionary<string, string> variables)
    {
        return PlaceholderPattern.Replace(input, match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
