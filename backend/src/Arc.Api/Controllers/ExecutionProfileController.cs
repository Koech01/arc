using Arc.Api.DTOs.Execution;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;


namespace Arc.Api.Controllers;
/// <summary>
/// Controller for execution performance profiling endpoints.
/// Provides detailed performance analysis including task metrics,
/// critical path analysis, and resource utilization patterns.
/// </summary>
[ApiController]
[Route("api/executions")]
public sealed class ExecutionProfileController : ControllerBase
{
    private readonly IExecutionProfiler _profiler;

    public ExecutionProfileController(IExecutionProfiler profiler)
    {
        _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
    }

    /// <summary>
    /// Generates a deterministic performance profile for the specified execution.
    /// Returns detailed metrics including task-level execution times, dependency wait times,
    /// critical path analysis, and resource utilization patterns.
    /// </summary>
    /// <param name="id">The execution ID to profile.</param>
    /// <returns>Complete performance profile or 404 if execution not found.</returns>
    [HttpGet("{id}/profile")]
    public async Task<ActionResult<ExecutionPerformanceProfileDto>> GetProfile(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Execution ID cannot be null or empty.");

        var profile = await _profiler.GenerateProfileAsync(id);
        if (profile == null)
            return NotFound($"Execution with ID '{id}' not found.");

        var dto = MapToDto(profile);
        return Ok(dto);
    }

    private static ExecutionPerformanceProfileDto MapToDto(ExecutionPerformanceProfile profile)
    {
        var taskMetricsDto = profile.TaskMetrics
            .Select(tm => new TaskPerformanceMetricsDto(
                tm.TaskId,
                tm.TaskName,
                tm.ExecutionOrder,
                tm.ExecutionTimeMs,
                tm.DependencyWaitTimeMs,
                tm.IsCriticalPath,
                tm.Dependencies
            ))
            .ToList();

        var criticalPathDto = new CriticalPathAnalysisDto(
            profile.CriticalPath.CriticalPathTaskIds,
            profile.CriticalPath.TotalCriticalPathTimeMs,
            profile.CriticalPath.CriticalPathPercentage
        );

        var resourceUtilizationDto = new ResourceUtilizationMetricsDto(
            profile.ResourceUtilization.TotalExecutionTimeMs,
            profile.ResourceUtilization.ParallelizableTimeMs,
            profile.ResourceUtilization.SequentialTimeMs,
            profile.ResourceUtilization.ParallelizationEfficiency,
            profile.ResourceUtilization.MaxConcurrentTasks,
            profile.ResourceUtilization.AverageTaskExecutionTimeMs
        );

        return new ExecutionPerformanceProfileDto(
            profile.ExecutionId,
            taskMetricsDto,
            criticalPathDto,
            resourceUtilizationDto,
            profile.ProfileGeneratedAtUtc
        );
    }
}