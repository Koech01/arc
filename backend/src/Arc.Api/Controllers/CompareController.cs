using Arc.Api.DTOs.Execution;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;


namespace Arc.Api.Controllers;
/// <summary>
/// Execution comparison and diff endpoint.
/// Allows clients to compare two executions and identify differences.
/// </summary>
[ApiController]
[Route("api/executions")]
public sealed class CompareController : ControllerBase
{
    private readonly IExecutionComparer _comparer;

    public CompareController(IExecutionComparer comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
    }

    /// <summary>
    /// Compares two executions deterministically.
    /// Returns detailed task-by-task comparison and aggregated diff metrics.
    /// </summary>
    /// <param name="request">Request containing two execution IDs to compare.</param>
    /// <returns>
    /// 200 OK with detailed comparison result if both executions found.
    /// 404 Not Found if either execution ID does not exist.
    /// 400 Bad Request if request is invalid.
    /// </returns>
    [HttpPost("compare")]
    public async Task<ActionResult<ExecutionComparisonResponseDto>> Compare([FromBody] ExecutionCompareRequestDto request)
    {
        if (request is null)
            return BadRequest("Request cannot be null.");

        if (string.IsNullOrWhiteSpace(request.ExecutionId1))
            return BadRequest("ExecutionId1 cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(request.ExecutionId2))
            return BadRequest("ExecutionId2 cannot be null or empty.");

        try
        {
            var comparisonResult = await _comparer.CompareAsync(request.ExecutionId1, request.ExecutionId2);

            if (comparisonResult is null)
                return NotFound($"One or both executions not found: '{request.ExecutionId1}' or '{request.ExecutionId2}'.");

            var response = new ExecutionComparisonResponseDto(
                ExecutionId1: comparisonResult.ExecutionId1,
                ExecutionId2: comparisonResult.ExecutionId2,
                TaskComparisons: comparisonResult.TaskComparisons
                    .Select(tc => new TaskComparisonItemDto(
                        TaskId: tc.TaskId,
                        ExecutionIndex: tc.ExecutionIndex,
                        Status1: tc.Status1,
                        Status2: tc.Status2,
                        Output1: tc.Output1,
                        Output2: tc.Output2,
                        ExecutionOrder1: tc.ExecutionOrder1,
                        ExecutionOrder2: tc.ExecutionOrder2,
                        IsDifferent: tc.IsDifferent
                    ))
                    .ToList(),
                Metrics: new ExecutionDiffMetricsDto(
                    TaskCount: comparisonResult.Metrics.TaskCount,
                    IdenticalTasks: comparisonResult.Metrics.IdenticalTasks,
                    DifferentTasks: comparisonResult.Metrics.DifferentTasks,
                    DivergencePointIndex: comparisonResult.Metrics.DivergencePointIndex,
                    SameTaskCount: comparisonResult.Metrics.SameTaskCount,
                    SameExecutionOrder: comparisonResult.Metrics.SameExecutionOrder,
                    SimilarityPercentage: comparisonResult.Metrics.SimilarityPercentage
                ),
                Summary: comparisonResult.Summary
            );

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}