namespace Arc.Api.DTOs.Execution;


/// <summary>
/// DTO for task mapping transformation rule.
/// </summary>
public sealed record TaskMappingRuleDto(
    string SourceTaskId,
    string TargetTaskId,
    string? TargetTaskName = null
);

/// <summary>
/// DTO for dependency rewiring transformation rule.
/// </summary>
public sealed record DependencyRewiringRuleDto(
    string TaskId,
    string[] NewDependencies
);

/// <summary>
/// Request DTO for execution transformation.
/// </summary>
public sealed record ExecutionTransformationRequestDto(
    string ExecutionId,
    TaskMappingRuleDto[] TaskMappings,
    DependencyRewiringRuleDto[] DependencyRewiring
);

/// <summary>
/// Response DTO for execution transformation.
/// </summary>
public sealed record ExecutionTransformationResponseDto(
    string TransformedExecutionId,
    TaskResultDto[] TransformedTasks,
    TransformationSummaryDto Summary
);

/// <summary>
/// Summary of transformation applied.
/// </summary>
public sealed record TransformationSummaryDto(
    string OriginalExecutionId,
    int TasksMapped,
    int DependenciesRewired,
    int TotalTransformedTasks
);