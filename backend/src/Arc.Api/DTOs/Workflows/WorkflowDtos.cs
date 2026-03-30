namespace Arc.Api.DTOs.Workflows;


public sealed class CreateWorkflowRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<WorkflowTaskDto> Tasks { get; set; } = new();
    public string TriggerType { get; set; } = string.Empty;
    public string? LLMConfigId { get; set; }
}

public sealed class WorkflowTaskDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string? Prompt { get; set; } // Optional task-specific prompt
    public string? LLMConfigId { get; set; } // Optional LLM configuration ID
    public Dictionary<string, string> Config { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    // For backward compatibility with tests and validators
    public List<string> DependsOn
    {
        get => Dependencies;
        set => Dependencies = value;
    }
}

public sealed class WorkflowResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class WorkflowListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class WorkflowDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<WorkflowTaskDto> Tasks { get; set; } = new();
    public string TriggerType { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}