using System.Text.Json;
using Arc.Domain.Models;
using Arc.Application.Execution;
using Arc.Application.Persistence;
using System.Text.RegularExpressions;
namespace Arc.Infrastructure.Persistence;


/// <summary>
/// PostgreSQL implementation of execution template store.
/// All operations are scoped to the owning user (multi-tenant).
/// </summary>
public sealed class PostgresExecutionTemplateStore : IExecutionTemplateStore
{
    private readonly IDatabaseContext _dbContext;
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    public PostgresExecutionTemplateStore(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ExecutionTemplate> CreateAsync(string name, string description, IReadOnlyList<WorkflowTask> tasks, string triggerType, UserId userId, string? llmConfigId = null)
    {
        var tasksJson = JsonSerializer.Serialize(tasks.Select(t => new TaskData(
            t.Id,
            t.Name,
            t.AgentType,
            t.Prompt,
            t.LLMConfigId,
            t.Config.ToDictionary(kv => kv.Key, kv => kv.Value),
            t.Dependencies.ToList()
        )).ToList(), JsonOptions);
        
        var createdAt = DateTime.UtcNow;

        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = @"
            INSERT INTO execution_templates (name, user_id, description, tasks_json, trigger_type, llm_config_id, created_at_utc, use_count)
            VALUES (@name, @userId, @description, @tasksJson, @triggerType, @llmConfigId, @createdAt, 0)";

        AddParameter(command, "@name", name.Trim().ToLowerInvariant());
        AddParameter(command, "@userId", userId.Value);
        AddParameter(command, "@description", description ?? (object)DBNull.Value);
        AddParameter(command, "@tasksJson", tasksJson);
        AddParameter(command, "@triggerType", triggerType);
        AddParameter(command, "@llmConfigId", llmConfigId ?? (object)DBNull.Value);
        AddParameter(command, "@createdAt", createdAt);

        await command.ExecuteNonQueryAsync();

        return new ExecutionTemplate(name, description ?? string.Empty, tasks, triggerType, llmConfigId, createdAt, 0);
    }

    public async Task<ExecutionTemplate?> GetAsync(string name, UserId userId)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = "SELECT name, description, tasks_json, trigger_type, llm_config_id, created_at_utc, use_count FROM execution_templates WHERE TRIM(name) = @name AND user_id = @userId";
        AddParameter(command, "@name", name.Trim().ToLowerInvariant());
        AddParameter(command, "@userId", userId.Value);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var tasksJson = reader.GetString(2);
        var taskDataList = JsonSerializer.Deserialize<List<TaskData>>(tasksJson, JsonOptions);
        var tasks = taskDataList!.Select(td => new WorkflowTask(
            td.Id,
            td.Name,
            td.AgentType,
            td.Prompt,
            td.LLMConfigId,
            td.Config ?? new Dictionary<string, string>(),
            td.Dependencies ?? new List<string>()
        )).ToList();
        
        return new ExecutionTemplate(
            reader.GetString(0),
            reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            tasks,
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetDateTime(5),
            reader.GetInt32(6)
        );
    }

    public async Task<IReadOnlyList<ExecutionTemplate>> ListAsync(UserId userId)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = "SELECT name, description, tasks_json, trigger_type, llm_config_id, created_at_utc, use_count FROM execution_templates WHERE user_id = @userId ORDER BY name ASC";
        AddParameter(command, "@userId", userId.Value);

        var templates = new List<ExecutionTemplate>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var tasksJson = reader.GetString(2);
            var taskDataList = JsonSerializer.Deserialize<List<TaskData>>(tasksJson, JsonOptions);
            var tasks = taskDataList!.Select(td => new WorkflowTask(
                td.Id,
                td.Name,
                td.AgentType,
                td.Prompt,
                td.LLMConfigId,
                td.Config ?? new Dictionary<string, string>(),
                td.Dependencies ?? new List<string>()
            )).ToList();
            
            templates.Add(new ExecutionTemplate(
                reader.GetString(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                tasks,
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetDateTime(5),
                reader.GetInt32(6)
            ));
        }

        return templates;
    }

    public async Task<bool> DeleteAsync(string name, UserId userId)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = "DELETE FROM execution_templates WHERE TRIM(name) = @name AND user_id = @userId";
        AddParameter(command, "@name", name.Trim().ToLowerInvariant());
        AddParameter(command, "@userId", userId.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<TemplateInstantiationResult?> InstantiateAsync(
        string templateName,
        UserId userId,
        Dictionary<string, string>? variables = null)
    {
        var template = await GetAsync(templateName, userId);
        if (template == null)
            return null;

        await IncrementUseCountAsync(templateName, userId);
        var runNumber = template.UseCount + 1;

        var tasks = template.Tasks;
        if (variables != null && variables.Count > 0)
            tasks = tasks.Select(task => SubstituteTaskVariables(task, variables)).ToList();

        return new TemplateInstantiationResult(templateName, tasks, template.TriggerType, template.LLMConfigId, template.CreatedAtUtc, runNumber);
    }

    public async Task<bool> UpdateAsync(
        string name,
        string description,
        IReadOnlyList<WorkflowTask> tasks,
        string triggerType,
        UserId userId,
        string? llmConfigId)
    {
        var tasksJson = JsonSerializer.Serialize(tasks.Select(t => new TaskData(
            t.Id,
            t.Name,
            t.AgentType,
            t.Prompt,
            t.LLMConfigId,
            t.Config.ToDictionary(kv => kv.Key, kv => kv.Value),
            t.Dependencies.ToList()
        )).ToList(), JsonOptions);

        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE execution_templates
            SET description   = @description,
                tasks_json    = @tasksJson,
                trigger_type  = @triggerType,
                llm_config_id = @llmConfigId
            WHERE TRIM(name) = @name AND user_id = @userId";

        AddParameter(command, "@name", name.Trim().ToLowerInvariant());
        AddParameter(command, "@userId", userId.Value);
        AddParameter(command, "@description", description ?? (object)DBNull.Value);
        AddParameter(command, "@tasksJson", tasksJson);
        AddParameter(command, "@triggerType", triggerType);
        AddParameter(command, "@llmConfigId", llmConfigId ?? (object)DBNull.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    private async Task IncrementUseCountAsync(string name, UserId userId)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "UPDATE execution_templates SET use_count = use_count + 1 WHERE TRIM(name) = @name AND user_id = @userId";
        AddParameter(command, "@name", name.Trim().ToLowerInvariant());
        AddParameter(command, "@userId", userId.Value);

        await command.ExecuteNonQueryAsync();
    }

    private static WorkflowTask SubstituteTaskVariables(WorkflowTask task, Dictionary<string, string> variables)
    {
        var substitutedId = SubstituteString(task.Id, variables);
        var substitutedName = SubstituteString(task.Name, variables);
        var substitutedPrompt = task.Prompt != null ? SubstituteString(task.Prompt, variables) : null;

        return new WorkflowTask(
            substitutedId,
            substitutedName,
            task.AgentType,
            substitutedPrompt,
            task.LLMConfigId,
            task.Config,
            task.Dependencies);
    }

    private static readonly Regex PlaceholderPattern = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    private static string SubstituteString(string input, Dictionary<string, string> variables)
    {
        return PlaceholderPattern.Replace(input, match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    private static void AddParameter(System.Data.IDbCommand command, string name, object value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }

    private sealed record TaskData(
        string Id,
        string Name,
        string AgentType,
        string? Prompt,
        string? LLMConfigId,
        Dictionary<string, string>? Config,
        List<string>? Dependencies
    );
}
