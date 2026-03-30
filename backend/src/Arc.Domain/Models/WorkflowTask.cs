using Arc.Domain.Exceptions;
namespace Arc.Domain.Models;


public sealed class WorkflowTask
{
    public string Id { get; }
    public string Name { get; }
    public string AgentType { get; }
    public string? Prompt { get; } // Optional task-specific prompt for LLM execution
    public string? LLMConfigId { get; } // Optional LLM configuration ID
    public IReadOnlyDictionary<string, string> Config { get; }
    public IReadOnlyList<string> Dependencies { get; }

    public WorkflowTask(
        string id,
        string name,
        string agentType,
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<string> dependencies)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new WorkflowException("Task ID cannot be empty");
        if (string.IsNullOrWhiteSpace(name))
            throw new WorkflowException("Task name cannot be empty");
        if (name.Length > 100)
            throw new WorkflowException("Task name cannot exceed 100 characters");
        if (string.IsNullOrWhiteSpace(agentType))
            throw new WorkflowException("Task agent type cannot be empty");
        if (!IsValidAgentType(agentType))
            throw new WorkflowException($"Invalid agent type: {agentType}");

        Id = id;
        Name = name;
        AgentType = agentType;
        Prompt = null;
        LLMConfigId = null;
        Config = config ?? new Dictionary<string, string>();
        Dependencies = dependencies ?? new List<string>();
    }

    public WorkflowTask(
        string id,
        string name,
        string agentType,
        string? prompt,
        string? llmConfigId,
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<string> dependencies)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new WorkflowException("Task ID cannot be empty");
        if (string.IsNullOrWhiteSpace(name))
            throw new WorkflowException("Task name cannot be empty");
        if (name.Length > 100)
            throw new WorkflowException("Task name cannot exceed 100 characters");
        if (string.IsNullOrWhiteSpace(agentType))
            throw new WorkflowException("Task agent type cannot be empty");
        if (!IsValidAgentType(agentType))
            throw new WorkflowException($"Invalid agent type: {agentType}");
        if (prompt != null && prompt.Length > 5000)
            throw new WorkflowException("Task prompt cannot exceed 5000 characters");

        Id = id;
        Name = name;
        AgentType = agentType;
        Prompt = string.IsNullOrWhiteSpace(prompt) ? null : prompt.Trim();
        LLMConfigId = string.IsNullOrWhiteSpace(llmConfigId) ? null : llmConfigId.Trim();
        Config = config ?? new Dictionary<string, string>();
        Dependencies = dependencies ?? new List<string>();
    }

    private static bool IsValidAgentType(string agentType)
    {
        return agentType is "http" or "python" or "sql" or "email" or "llm";
    }
}