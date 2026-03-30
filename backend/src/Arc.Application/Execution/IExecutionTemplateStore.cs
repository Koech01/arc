using Arc.Domain.Models;
namespace Arc.Application.Execution;


/// <summary>
/// Execution template definition storing workflow pattern.
/// </summary>
public sealed record ExecutionTemplate(
    string Name,
    string Description,
    IReadOnlyList<WorkflowTask> Tasks,
    string TriggerType,
    string? LLMConfigId,
    DateTime CreatedAtUtc,
    int UseCount
);

/// <summary>
/// Template instantiation result with variable substitution applied.
/// Returns a workflow ready to be saved and executed.
/// RunNumber is the post-increment UseCount value - starts at 1 on first instantiation.
/// </summary>
public sealed record TemplateInstantiationResult(
    string TemplateName,
    IReadOnlyList<WorkflowTask> InstantiatedTasks,
    string TriggerType,
    string? LLMConfigId,
    DateTime CreatedAtUtc,
    int RunNumber
);

/// <summary>
/// Deterministic execution template store interface.
/// All operations are scoped to the owning user (multi-tenant).
/// </summary>
public interface IExecutionTemplateStore
{
    Task<ExecutionTemplate> CreateAsync(string name, string description, IReadOnlyList<WorkflowTask> tasks, string triggerType, UserId userId, string? llmConfigId = null);

    Task<ExecutionTemplate?> GetAsync(string name, UserId userId);

    Task<IReadOnlyList<ExecutionTemplate>> ListAsync(UserId userId);

    Task<bool> DeleteAsync(string name, UserId userId);

    Task<TemplateInstantiationResult?> InstantiateAsync(string templateName, UserId userId, Dictionary<string, string>? variables = null);

    Task<bool> UpdateAsync(
        string name,
        string description,
        IReadOnlyList<WorkflowTask> tasks,
        string triggerType,
        UserId userId,
        string? llmConfigId);
}