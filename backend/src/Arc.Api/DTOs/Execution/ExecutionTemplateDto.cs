namespace Arc.Api.DTOs.Execution;


/// <summary>
/// Request DTO for creating an execution template.
/// </summary>
public sealed record CreateExecutionTemplateRequestDto(
    string Name,
    string Description,
    List<TemplateTaskDto> Tasks,
    string TriggerType,
    string? LLMConfigId
);

/// <summary>
/// Template task definition.
/// </summary>
public sealed record TemplateTaskDto(
    string Id,
    string Name,
    string AgentType,
    string? Prompt,
    string? LLMConfigId,
    Dictionary<string, string>? Config,
    List<string>? Dependencies
);

/// <summary>
/// Execution template metadata item for list response.
/// </summary>
public sealed record ExecutionTemplateMetadataDto(
    string Name,
    string Description,
    DateTime CreatedAtUtc,
    int UseCount,
    string? LLMConfigId
);

/// <summary>
/// Execution template response DTO.
/// </summary>
public sealed record ExecutionTemplateResponseDto(
    string Name,
    string Description,
    DateTime CreatedAtUtc,
    int UseCount
);

/// <summary>
/// Execution template detail DTO with full task information.
/// </summary>
public sealed record ExecutionTemplateDetailDto(
    string Name,
    string Description,
    DateTime CreatedAtUtc,
    int UseCount,
    string TriggerType,
    string? LLMConfigId,
    List<TemplateTaskDto> Tasks
);

/// <summary>
/// Template instantiation request with variable substitution.
/// LLMConfigId overrides the template-level config for the created workflow.
/// </summary>
public sealed record TemplateInstantiationRequestDto(
    Dictionary<string, string>? Variables,
    string? LLMConfigId = null
);

/// <summary>
/// Template instantiation response with workflow ID.
/// </summary>
public sealed record TemplateInstantiationResponseDto(
    string WorkflowId,
    string WorkflowName
);