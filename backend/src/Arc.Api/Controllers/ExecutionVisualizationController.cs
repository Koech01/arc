using Arc.Api.DTOs.Execution;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;


namespace Arc.Api.Controllers;
/// <summary>
/// Controller for execution workflow visualization endpoints.
/// Provides structured visualization data including dependency graphs,
/// execution timelines, critical path highlighting, and resource allocation.
/// </summary>
[ApiController]
[Route("api/executions")]
public sealed class ExecutionVisualizationController : ControllerBase
{
    private readonly IExecutionVisualizer _visualizer;

    public ExecutionVisualizationController(IExecutionVisualizer visualizer)
    {
        _visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
    }

    /// <summary>
    /// Generates deterministic visualization data for the specified execution.
    /// Returns structured data for task dependency graph, execution timeline,
    /// critical path highlighting, and resource allocation over time.
    /// </summary>
    /// <param name="id">The execution ID to visualize.</param>
    /// <returns>Complete visualization data or 404 if execution not found.</returns>
    [HttpGet("{id}/visualization")]
    public async Task<ActionResult<ExecutionVisualizationDto>> GetVisualization(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Execution ID cannot be null or empty.");

        var visualization = await _visualizer.GenerateVisualizationAsync(id);
        if (visualization == null)
            return NotFound($"Execution with ID '{id}' not found.");

        var dto = MapToDto(visualization);
        return Ok(dto);
    }

    private static ExecutionVisualizationDto MapToDto(ExecutionVisualization visualization)
    {
        var dependencyGraphDto = visualization.DependencyGraph
            .Select(node => new TaskGraphNodeDto(
                node.TaskId,
                node.TaskName,
                node.ExecutionOrder,
                node.Status,
                node.Dependencies,
                node.IsCriticalPath,
                node.ExecutionTimeMs
            ))
            .ToList();

        var timelineDto = visualization.ExecutionTimeline
            .Select(evt => new TimelineEventDto(
                evt.TaskId,
                evt.TaskName,
                evt.StartTime,
                evt.EndTime,
                evt.DurationMs,
                evt.EventType,
                evt.IsCriticalPath
            ))
            .ToList();

        var resourceAllocationDto = visualization.ResourceAllocation
            .Select(snapshot => new ResourceSnapshotDto(
                snapshot.Timestamp,
                snapshot.ActiveTasks,
                snapshot.RunningTaskIds
            ))
            .ToList();

        return new ExecutionVisualizationDto(
            visualization.ExecutionId,
            dependencyGraphDto,
            timelineDto,
            visualization.CriticalPathTaskIds,
            resourceAllocationDto,
            visualization.GeneratedAtUtc
        );
    }
}