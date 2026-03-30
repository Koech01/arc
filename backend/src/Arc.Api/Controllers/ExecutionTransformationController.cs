using Arc.Api.DTOs;
using Arc.Api.DTOs.Execution;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;


namespace Arc.Api.Controllers;
[ApiController]
[Route("api/executions")]
public sealed class ExecutionTransformationController : ControllerBase
{
    private readonly IExecutionTransformer _executionTransformer;

    public ExecutionTransformationController(IExecutionTransformer executionTransformer)
    {
        _executionTransformer = executionTransformer ?? throw new ArgumentNullException(nameof(executionTransformer));
    }

    /// <summary>
    /// Transforms an execution based on provided transformation rules.
    /// </summary>
    /// <param name="request">Transformation request with execution ID and rules.</param>
    /// <returns>Transformed execution with new deterministic ID.</returns>
    [HttpPost("transform")]
    public async Task<ActionResult<ExecutionTransformationResponseDto>> TransformExecution(
        [FromBody] ExecutionTransformationRequestDto request)
    {
        if (request is null)
            return BadRequest("Request cannot be null.");

        if (string.IsNullOrWhiteSpace(request.ExecutionId))
            return BadRequest("ExecutionId is required.");

        try
        {
            // Convert DTOs to domain models
            var taskMappings = request.TaskMappings?.Select(tm => new TaskMappingRule(
                tm.SourceTaskId,
                tm.TargetTaskId,
                tm.TargetTaskName
            )).ToArray() ?? Array.Empty<TaskMappingRule>();

            var dependencyRewiring = request.DependencyRewiring?.Select(dr => new DependencyRewiringRule(
                dr.TaskId,
                dr.NewDependencies
            )).ToArray() ?? Array.Empty<DependencyRewiringRule>();

            var transformationRules = new ExecutionTransformationRules(taskMappings, dependencyRewiring);

            // Apply transformation
            var result = await _executionTransformer.TransformAsync(request.ExecutionId, transformationRules);

            // Convert to response DTO
            var transformedTasks = result.TransformedExecution.Tasks
                .OrderBy(t => t.ExecutionOrder)
                .Select(t => new TaskResultDto(
                    TaskId: t.TaskId,
                    TaskName: t.TaskName,
                    ExecutionOrder: t.ExecutionOrder,
                    Status: t.Status.ToString()
                ))
                .ToArray();

            var summary = new TransformationSummaryDto(
                OriginalExecutionId: request.ExecutionId,
                TasksMapped: taskMappings.Length,
                DependenciesRewired: dependencyRewiring.Length,
                TotalTransformedTasks: transformedTasks.Length
            );

            var response = new ExecutionTransformationResponseDto(
                TransformedExecutionId: result.TransformedExecutionId,
                TransformedTasks: transformedTasks,
                Summary: summary
            );

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}