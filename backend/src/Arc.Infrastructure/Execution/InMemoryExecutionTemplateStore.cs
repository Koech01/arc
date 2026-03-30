using Arc.Domain.Models;
using Arc.Application.Execution;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
namespace Arc.Infrastructure.Execution;


/// <summary>
/// In-memory execution template store with deterministic behavior.
/// All operations are scoped to the owning user (multi-tenant).
/// </summary>
public sealed class InMemoryExecutionTemplateStore : IExecutionTemplateStore
{
    private readonly ConcurrentDictionary<(string Name, Guid UserId), (ExecutionTemplate template, int useCount)> _templates = new();

    public Task<ExecutionTemplate> CreateAsync(string name, string description, IReadOnlyList<WorkflowTask> tasks, string triggerType, string? llmConfigId = null)
        => CreateAsync(name, description, tasks, triggerType, UserId.Anonymous, llmConfigId);

    public Task<ExecutionTemplate> CreateAsync(string name, string description, IReadOnlyList<WorkflowTask> tasks, string triggerType, UserId userId, string? llmConfigId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be null or empty.", nameof(name));

        if (tasks is null || tasks.Count == 0)
            throw new ArgumentException("Template must have at least one task.", nameof(tasks));

        if (string.IsNullOrWhiteSpace(triggerType))
            throw new ArgumentException("Template trigger type cannot be null or empty.", nameof(triggerType));

        var template = new ExecutionTemplate(
            Name: name.Trim(),
            Description: description ?? "",
            Tasks: tasks,
            TriggerType: triggerType,
            LLMConfigId: llmConfigId,
            CreatedAtUtc: DateTime.UtcNow,
            UseCount: 0
        );

        if (!_templates.TryAdd((name.Trim(), userId.Value), (template, 0)))
            throw new InvalidOperationException($"Template '{name}' already exists.");

        return Task.FromResult(template);
    }

    public Task<ExecutionTemplate?> GetAsync(string name)
        => GetAsync(name, UserId.Anonymous);

    public Task<ExecutionTemplate?> GetAsync(string name, UserId userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult<ExecutionTemplate?>(null);

        _templates.TryGetValue((name.Trim(), userId.Value), out var entry);
        if (entry.template is null) return Task.FromResult<ExecutionTemplate?>(null);

        // Return template with current UseCount reflected
        var t = entry.template with { UseCount = entry.useCount };
        return Task.FromResult<ExecutionTemplate?>(t);
    }

    public Task<IReadOnlyList<ExecutionTemplate>> ListAsync()
        => ListAsync(UserId.Anonymous);

    public Task<IReadOnlyList<ExecutionTemplate>> ListAsync(UserId userId)
    {
        var templates = _templates
            .Where(kv => kv.Key.UserId == userId.Value)
            .OrderBy(kv => kv.Key.Name)
            .Select(kv => kv.Value.template)
            .ToList();

        return Task.FromResult<IReadOnlyList<ExecutionTemplate>>(templates);
    }

    public Task<bool> DeleteAsync(string name)
        => DeleteAsync(name, UserId.Anonymous);

    public Task<bool> DeleteAsync(string name, UserId userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be null or empty.", nameof(name));

        var removed = _templates.TryRemove((name.Trim(), userId.Value), out _);
        return Task.FromResult(removed);
    }

    public Task<bool> UpdateAsync(
        string name,
        string description,
        IReadOnlyList<WorkflowTask> tasks,
        string triggerType,
        UserId userId,
        string? llmConfigId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be null or empty.", nameof(name));

        var key = (name.Trim(), userId.Value);
        if (!_templates.TryGetValue(key, out var existing))
            return Task.FromResult(false);

        var updated = new ExecutionTemplate(
            Name: existing.template.Name,
            Description: description ?? string.Empty,
            Tasks: tasks,
            TriggerType: triggerType,
            LLMConfigId: llmConfigId,
            CreatedAtUtc: existing.template.CreatedAtUtc,
            UseCount: existing.useCount);

        _templates[key] = (updated, existing.useCount);
        return Task.FromResult(true);
    }

    public Task<TemplateInstantiationResult?> InstantiateAsync(string templateName, Dictionary<string, string>? variables = null)
        => InstantiateAsync(templateName, UserId.Anonymous, variables);

    public async Task<TemplateInstantiationResult?> InstantiateAsync(string templateName, UserId userId, Dictionary<string, string>? variables = null)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be null or empty.", nameof(templateName));

        var template = await GetAsync(templateName, userId);
        if (template is null)
            return null;

        var instantiatedTasks = SubstituteVariables(template.Tasks, variables ?? new());

        var key = (templateName.Trim(), userId.Value);
        _templates.TryGetValue(key, out var beforeUpdate);
        var runNumber = (beforeUpdate.template is not null ? beforeUpdate.useCount : 0) + 1;

        _templates.AddOrUpdate(
            key,
            (template, 1),
            (k, existing) => (existing.template, existing.useCount + 1)
        );

        return new TemplateInstantiationResult(
            TemplateName: templateName,
            InstantiatedTasks: instantiatedTasks,
            TriggerType: template.TriggerType,
            LLMConfigId: template.LLMConfigId,
            CreatedAtUtc: DateTime.UtcNow,
            RunNumber: runNumber
        );
    }

    private static IReadOnlyList<WorkflowTask> SubstituteVariables(IReadOnlyList<WorkflowTask> tasks, Dictionary<string, string> variables)
    {
        if (variables.Count == 0)
            return tasks;

        return tasks.Select(task => SubstituteTaskVariables(task, variables)).ToList();
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

    private static readonly Regex PlaceholderPattern = new Regex(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled);

    private static string SubstituteString(string input, Dictionary<string, string> variables)
    {
        return PlaceholderPattern.Replace(input, match =>
        {
            var key = match.Groups[1].Value;
            // Case-insensitive lookup
            var found = variables.FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
            return found.Key != null ? found.Value : match.Value;
        });
    }
}
