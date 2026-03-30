using Arc.Domain.Exceptions;
namespace Arc.Domain.Models;

public sealed class Workflow
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<WorkflowTask> Tasks { get; }
    public string TriggerType { get; }
    public string? LLMConfigId { get; }
    public UserId CreatedBy { get; }
    public DateTime CreatedAt { get; }

    public Workflow(
        string id,
        string name,
        string description,
        IReadOnlyList<WorkflowTask> tasks,
        string triggerType,
        UserId createdBy,
        DateTime createdAt,
        string? llmConfigId = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new WorkflowException("Workflow ID cannot be empty");
        if (string.IsNullOrWhiteSpace(name))
            throw new WorkflowException("Workflow name cannot be empty");
        if (name.Length > 200)
            throw new WorkflowException("Workflow name cannot exceed 200 characters");
        if (description?.Length > 1000)
            throw new WorkflowException("Workflow description cannot exceed 1000 characters");
        if (tasks == null || tasks.Count == 0)
            throw new WorkflowException("Workflow must have at least one task");
        if (string.IsNullOrWhiteSpace(triggerType))
            throw new WorkflowException("Workflow trigger type cannot be empty");
        if (!IsValidTriggerType(triggerType))
            throw new WorkflowException($"Invalid trigger type: {triggerType}");
        if (createdBy.Value == Guid.Empty)
            throw new WorkflowException("Workflow must have a creator");

        ValidateTaskIds(tasks);
        ValidateDependencies(tasks);

        Id = id;
        Name = name;
        Description = description ?? string.Empty;
        Tasks = tasks;
        TriggerType = triggerType;
        LLMConfigId = llmConfigId;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    private static bool IsValidTriggerType(string triggerType)
    {
        return triggerType is "manual" or "scheduled" or "webhook";
    }

    private static void ValidateTaskIds(IReadOnlyList<WorkflowTask> tasks)
    {
        var taskIds = tasks.Select(t => t.Id).ToList();
        var duplicates = taskIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Any())
            throw new WorkflowException($"Duplicate task IDs found: {string.Join(", ", duplicates)}");
    }

    private static void ValidateDependencies(IReadOnlyList<WorkflowTask> tasks)
    {
        var taskIds = new HashSet<string>(tasks.Select(t => t.Id));
        foreach (var task in tasks)
        {
            foreach (var dep in task.Dependencies)
            {
                if (!taskIds.Contains(dep))
                    throw new WorkflowException($"Task '{task.Id}' references non-existent dependency '{dep}'");
            }
        }

        DetectCycles(tasks);
    }

    private static void DetectCycles(IReadOnlyList<WorkflowTask> tasks)
    {
        var graph = tasks.ToDictionary(t => t.Id, t => t.Dependencies.ToList());
        var visited = new HashSet<string>();
        var recStack = new HashSet<string>();

        foreach (var taskId in graph.Keys)
        {
            if (HasCycle(taskId, graph, visited, recStack))
                throw new WorkflowException("Workflow contains circular dependencies");
        }
    }

    private static bool HasCycle(string taskId, Dictionary<string, List<string>> graph, HashSet<string> visited, HashSet<string> recStack)
    {
        if (recStack.Contains(taskId))
            return true;
        if (visited.Contains(taskId))
            return false;

        visited.Add(taskId);
        recStack.Add(taskId);

        if (graph.TryGetValue(taskId, out var dependencies))
        {
            foreach (var dep in dependencies)
            {
                if (HasCycle(dep, graph, visited, recStack))
                    return true;
            }
        }

        recStack.Remove(taskId);
        return false;
    }
}