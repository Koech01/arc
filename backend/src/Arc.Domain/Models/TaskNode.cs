using Arc.Domain.Exceptions;
namespace Arc.Domain.Models;
using System.Text.Json.Serialization;


public sealed class TaskNode
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string? Prompt { get; init; } // Optional task-specific prompt for LLM execution
    public string? LLMConfigId { get; init; } // Optional LLM configuration ID
    public IReadOnlyCollection<string> DependsOn { get; init; } // list of task IDs that must run first

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    public TaskNode()
    {
        Id = string.Empty;
        Name = string.Empty;
        Prompt = null;
        LLMConfigId = null;
        DependsOn = Array.Empty<string>();
    }

    public TaskNode(
        string id,
        string name,
        string? prompt = null,
        string? llmConfigId = null,
        IEnumerable<string>? dependsOn = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new TaskNodeInvalidException("TaskNode id cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(name))
            throw new TaskNodeInvalidException("TaskNode name cannot be null or empty.");

        if (prompt != null && prompt.Length > 5000)
            throw new TaskNodeInvalidException("TaskNode prompt cannot exceed 5000 characters.");

        Id = id.Trim();
        Name = name.Trim();
        Prompt = string.IsNullOrWhiteSpace(prompt) ? null : prompt.Trim();
        LLMConfigId = string.IsNullOrWhiteSpace(llmConfigId) ? null : llmConfigId.Trim();
        DependsOn = dependsOn?.Distinct().ToArray() ?? Array.Empty<string>();

        if (DependsOn.Contains(Id))
            throw new TaskNodeInvalidException("TaskNode cannot depend on itself.");
    }
}

public sealed class TaskNodeInvalidException : DomainException
{
    public TaskNodeInvalidException(string message) : base(message)
    {
    }
}