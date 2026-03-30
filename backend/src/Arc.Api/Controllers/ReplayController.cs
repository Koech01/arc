using Arc.Api.DTOs;
using Arc.Api.DTOs.Execution;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;


namespace Arc.Api.Controllers;

/// <summary>
/// Deterministic execution replay endpoint.
/// Allows clients to reconstruct and inspect execution state without re-execution.
/// </summary>
[ApiController]
[Route("api/replay")]
public sealed class ReplayController : ControllerBase
{
    private readonly IExecutionReplayer _replayer;

    public ReplayController(IExecutionReplayer replayer)
    {
        _replayer = replayer ?? throw new ArgumentNullException(nameof(replayer));
    }

    /// <summary>
    /// Replays a stored execution deterministically.
    /// Returns the original task results and complete audit trace.
    /// </summary>
    /// <param name="executionId">The deterministic execution ID to replay.</param>
    /// <returns>
    /// 200 OK with execution replay data if found.
    /// 404 Not Found if execution ID does not exist.
    /// 400 Bad Request if execution ID is invalid.
    /// </returns>
    [HttpGet("{executionId}")]
    public async Task<ActionResult<ExecutionReplayResponseDto>> Replay(string executionId)
    {
        if (string.IsNullOrWhiteSpace(executionId))
        {
            return BadRequest("ExecutionId cannot be null or empty.");
        }

        var replayResult = await _replayer.ReplayAsync(executionId);
        if (replayResult is null)
        {
            return NotFound($"No execution found with ID '{executionId}'.");
        }

        var response = new ExecutionReplayResponseDto(
            ExecutionId: replayResult.ExecutionId,
            Tasks: replayResult.Tasks
                .OrderBy(t => t.ExecutionOrder)
                .Select(t => new TaskResultDto(
                    TaskId: t.TaskId,
                    TaskName: t.TaskName,
                    ExecutionOrder: t.ExecutionOrder,
                    Status: t.Status.ToString()
                ))
                .ToArray(),
            AuditTrace: replayResult.AuditTrace
                .Select(entry => new ReplayAuditEntryDto(
                    Sequence: entry.Sequence,
                    TimestampUtc: entry.TimestampUtc,
                    EventType: entry.EventType,
                    TaskId: entry.TaskId,
                    Message: entry.Message
                ))
                .ToArray()
        );

        return Ok(response);
    }
}
