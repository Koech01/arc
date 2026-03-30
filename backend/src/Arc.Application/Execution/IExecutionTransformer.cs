using Arc.Domain.Models;
using Arc.Application.Results;
namespace Arc.Application.Execution;


/// <summary>
/// Transformation rule for task mapping.
/// </summary>
public sealed record TaskMappingRule(
    string SourceTaskId,
    string TargetTaskId,
    string? TargetTaskName = null
);

/// <summary>
/// Transformation rule for dependency rewiring.
/// </summary>
public sealed record DependencyRewiringRule(
    string TaskId,
    IReadOnlyCollection<string> NewDependencies
);

/// <summary>
/// Transformation rules for execution schema transformation.
/// </summary>
public sealed record ExecutionTransformationRules(
    IReadOnlyCollection<TaskMappingRule> TaskMappings,
    IReadOnlyCollection<DependencyRewiringRule> DependencyRewiring
);

/// <summary>
/// Result of execution transformation.
/// </summary>
public sealed record ExecutionTransformationResult(
    string TransformedExecutionId,
    ExecutionGraph TransformedGraph,
    ExecutionResult TransformedExecution
);

/// <summary>
/// Transforms execution schemas deterministically.
/// </summary>
public interface IExecutionTransformer
{
    /// <summary>
    /// Transforms an execution based on the provided rules.
    /// Returns a new execution with deterministic ID generation.
    /// </summary>
    /// <param name="executionId">Source execution ID to transform.</param>
    /// <param name="transformationRules">Rules to apply for transformation.</param>
    /// <returns>Transformed execution result with new execution ID.</returns>
    Task<ExecutionTransformationResult> TransformAsync(string executionId, ExecutionTransformationRules transformationRules);
}