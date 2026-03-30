using Arc.Api.DTOs;
using Arc.Api.Authorization;
using Arc.Application.Results;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;
using Arc.Application.Orchestration;


[ApiController]
[Route("api/execute")]
[RequireUserOrAdmin]
public sealed class ExecutionController : ControllerBase
{
    private readonly IOrchestrator _orchestrator;
    private readonly IExecutionResultStore _executionResultStore;

    public ExecutionController(
        IOrchestrator orchestrator,
        IExecutionResultStore executionResultStore)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _executionResultStore = executionResultStore ?? throw new ArgumentNullException(nameof(executionResultStore));
    }

    [HttpPost]
    public async Task<ActionResult<ExecuteResponseDto>> Execute([FromBody] ExecuteRequestDto request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var result = _orchestrator.Execute(request.Input);
        var executionId = result.ExecutionId;

        await _executionResultStore.StoreAsync(executionId, result);

        var response = new ExecuteResponseDto(
            ExecutionId: executionId,
            Tasks: result.Tasks
                .OrderBy(t => t.ExecutionOrder)
                .Select(t => new TaskResultDto(
                    TaskId: t.TaskId,
                    TaskName: t.TaskName,
                    ExecutionOrder: t.ExecutionOrder,
                    Status: t.Status.ToString()
                ))
                .ToArray()
        );

        return Ok(response);
    }

    [HttpGet("{executionId}")]
    public async Task<ActionResult<ExecuteResponseDto>> GetExecution(string executionId)
    {
        var result = await _executionResultStore.GetAsync(executionId);
        if (result is null)
            return NotFound();

        var response = new ExecuteResponseDto(
            ExecutionId: executionId,
            Tasks: result.Tasks
                .OrderBy(t => t.ExecutionOrder)
                .Select(t => new TaskResultDto(
                    TaskId: t.TaskId,
                    TaskName: t.TaskName,
                    ExecutionOrder: t.ExecutionOrder,
                    Status: t.Status.ToString()
                ))
                .ToArray()
        );

        return Ok(response);
    }
}
