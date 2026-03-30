using Npgsql;
using System.Text.Json;
using Arc.Domain.Models;
using Arc.Application.Workflows;
using Arc.Application.Persistence;
namespace Arc.Infrastructure.Workflows;


public sealed class PostgresWorkflowRepository : IWorkflowRepository
{
    private readonly IDatabaseContext _dbContext;

    public PostgresWorkflowRepository(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Workflow> CreateAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        await _dbContext.EnsureInitializedAsync(cancellationToken);

        const string sql = @"
            INSERT INTO workflows (id, name, description, tasks_json, trigger_type, llm_config_id, created_by, created_at)
            VALUES (@id, @name, @description, @tasks_json, @trigger_type, @llm_config_id, @created_by, @created_at)";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", workflow.Id);
        cmd.Parameters.AddWithValue("name", workflow.Name);
        cmd.Parameters.AddWithValue("description", workflow.Description);
        cmd.Parameters.AddWithValue("tasks_json", SerializeTasks(workflow.Tasks));
        cmd.Parameters.AddWithValue("trigger_type", workflow.TriggerType);
        cmd.Parameters.AddWithValue("llm_config_id", (object?)workflow.LLMConfigId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("created_by", workflow.CreatedBy.Value);
        cmd.Parameters.AddWithValue("created_at", workflow.CreatedAt);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return workflow;
    }

    public async Task<Workflow?> GetByIdAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        await _dbContext.EnsureInitializedAsync(cancellationToken);

        const string sql = @"
            SELECT id, name, description, tasks_json, trigger_type, llm_config_id, created_by, created_at
            FROM workflows
            WHERE id = @id";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", workflowId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapToWorkflow(reader);
    }

    public async Task<Workflow?> GetByNameAsync(string name, UserId userId, CancellationToken cancellationToken = default)
    {
        await _dbContext.EnsureInitializedAsync(cancellationToken);

        const string sql = @"
            SELECT id, name, description, tasks_json, trigger_type, llm_config_id, created_by, created_at
            FROM workflows
            WHERE LOWER(name) = LOWER(@name) AND created_by = @created_by::uuid";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("created_by", userId.Value.ToString());

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapToWorkflow(reader);
    }

    public async Task<IReadOnlyList<Workflow>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await _dbContext.EnsureInitializedAsync(cancellationToken);

        const string sql = @"
            SELECT id, name, description, tasks_json, trigger_type, llm_config_id, created_by, created_at
            FROM workflows
            WHERE created_by = @created_by
            ORDER BY created_at DESC";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("created_by", userId.Value);

        var workflows = new List<Workflow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            workflows.Add(MapToWorkflow(reader));
        }

        return workflows;
    }

    public async Task<bool> DeleteAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        await _dbContext.EnsureInitializedAsync(cancellationToken);

        const string sql = "DELETE FROM workflows WHERE id = @id";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", workflowId);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    private static string SerializeTasks(IReadOnlyList<WorkflowTask> tasks)
    {
        var taskDtos = tasks.Select(t => new
        {
            id = t.Id,
            name = t.Name,
            agentType = t.AgentType,
            prompt = t.Prompt,
            llmConfigId = t.LLMConfigId,
            config = t.Config,
            dependencies = t.Dependencies
        }).ToList();

        return JsonSerializer.Serialize(taskDtos);
    }

    private static Workflow MapToWorkflow(NpgsqlDataReader reader)
    {
        var id = reader.GetString(0);
        var name = reader.GetString(1);
        var description = reader.GetString(2);
        var tasksJson = reader.GetString(3);
        var triggerType = reader.GetString(4);
        var llmConfigId = reader.IsDBNull(5) ? null : reader.GetString(5);
        var createdBy = new UserId(reader.GetGuid(6));
        var createdAt = reader.GetDateTime(7);

        var tasks = DeserializeTasks(tasksJson);

        return new Workflow(id, name, description, tasks, triggerType, createdBy, createdAt, llmConfigId);
    }

    private static IReadOnlyList<WorkflowTask> DeserializeTasks(string tasksJson)
    {
        var taskDtos = JsonSerializer.Deserialize<List<TaskDto>>(tasksJson) ?? new List<TaskDto>();
        return taskDtos.Select(dto => new WorkflowTask(
            dto.id,
            dto.name,
            dto.agentType,
            dto.prompt,
            dto.llmConfigId,
            dto.config ?? new Dictionary<string, string>(),
            dto.dependencies ?? new List<string>()
        )).ToList();
    }

    private sealed class TaskDto
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string agentType { get; set; } = string.Empty;
        public string? prompt { get; set; }
        public string? llmConfigId { get; set; }
        public Dictionary<string, string>? config { get; set; }
        public List<string>? dependencies { get; set; }
    }
}