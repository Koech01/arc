using Arc.Api.DTOs;
using Arc.Api.DTOs.Execution;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;


namespace Arc.Api.Controllers;
/// <summary>
/// Deterministic batch execution endpoint.
/// Allows clients to execute multiple task graphs in a single request
/// with aggregated performance metrics and deterministic batch ID.
/// </summary>
[ApiController]
[Route("api/batch")]
public sealed class BatchController : ControllerBase
{
    private readonly IBatchExecutor _batchExecutor;

    public BatchController(IBatchExecutor batchExecutor)
    {
        _batchExecutor = batchExecutor ?? throw new ArgumentNullException(nameof(batchExecutor));
    }

    /// <summary>
    /// Executes multiple task graphs deterministically in batch.
    /// </summary>
    /// <param name="request">Batch request containing array of execution inputs.</param>
    /// <returns>
    /// 200 OK with batch results including all executions and aggregated metrics.
    /// 400 Bad Request if request is null or empty.
    /// </returns>
    [HttpPost]
    public async Task<ActionResult<BatchExecuteResponseDto>> ExecuteBatch([FromBody] BatchExecuteRequestDto request)
    {
        if (request is null || request.Executions.Count == 0)
        {
            return BadRequest("Batch request must contain at least one execution.");
        }

        var inputs = request.Executions.Select(e => e.Input).ToArray();
        var batchResult = await _batchExecutor.ExecuteBatchAsync(inputs);

        var response = new BatchExecuteResponseDto(
            BatchId: batchResult.BatchId,
            CreatedAtUtc: batchResult.CreatedAtUtc,
            Executions: batchResult.Executions
                .Select(execution => new BatchExecutionResponseItem(
                    Index: execution.Index,
                    ExecutionId: execution.ExecutionId,
                    Tasks: execution.Tasks
                        .OrderBy(t => t.ExecutionOrder)
                        .Select(t => new TaskResultDto(
                            TaskId: t.TaskId,
                            TaskName: t.TaskName,
                            ExecutionOrder: t.ExecutionOrder,
                            Status: t.Status.ToString()
                        ))
                        .ToArray(),
                    ExecutionTimeMs: execution.ExecutionTimeMs,
                    Status: execution.Status
                ))
                .ToArray(),
            TotalExecutionTimeMs: batchResult.TotalExecutionTimeMs,
            AverageExecutionTimeMs: batchResult.AverageExecutionTimeMs,
            SuccessCount: batchResult.SuccessCount,
            FailureCount: batchResult.FailureCount
        );

        return Ok(response);
    }
}