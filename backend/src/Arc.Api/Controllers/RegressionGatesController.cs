using Arc.Domain.Models;
namespace Arc.Api.Controllers;
using Arc.Application.Identity;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;
using Arc.Api.DTOs.RegressionGates;
using Arc.Application.RegressionGates;
using Microsoft.AspNetCore.Authorization;


/// <summary>
/// API endpoints for regression gate management and testing.
/// Enables deterministic "golden execution" baseline testing for safe change management.
/// </summary>
[ApiController]
[Route("api/regression-gates")]
[Authorize]
public sealed class RegressionGatesController : ControllerBase
{
    private readonly IRegressionGateRepository _gateRepository;
    private readonly IRegressionGateService _gateService;
    private readonly IGoldenExecutionStore _goldenStore;
    private readonly IExecutionResultStore _executionStore;
    private readonly IUserContext _userContext;
    private readonly ILogger<RegressionGatesController> _logger;

    public RegressionGatesController(
        IRegressionGateRepository gateRepository,
        IRegressionGateService gateService,
        IGoldenExecutionStore goldenStore,
        IExecutionResultStore executionStore,
        IUserContext userContext,
        ILogger<RegressionGatesController> logger)
    {
        _gateRepository = gateRepository ?? throw new ArgumentNullException(nameof(gateRepository));
        _gateService = gateService ?? throw new ArgumentNullException(nameof(gateService));
        _goldenStore = goldenStore ?? throw new ArgumentNullException(nameof(goldenStore));
        _executionStore = executionStore ?? throw new ArgumentNullException(nameof(executionStore));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new regression gate.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RegressionGateResponseDto>> CreateGate(
        [FromBody] CreateRegressionGateRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = _userContext.CurrentUserId;

        // Verify golden execution exists and is owned by user
        var goldenExecution = await _executionStore.GetAsync(request.GoldenExecutionId);
        if (goldenExecution == null)
        {
            return NotFound(new { message = $"Golden execution {request.GoldenExecutionId} not found" });
        }

        if (goldenExecution.UserId != userId)
        {
            return Forbid();
        }

        // Map DTOs to domain models
        var rules = request.Rules.Select(r => new DivergenceRule(
            DivergenceRuleTypeExtensions.FromStringValue(r.Type),
            r.Threshold
        )).ToList();

        var gate = new RegressionGate(
            RegressionGateId.NewId(),
            userId,
            request.Name,
            new GoldenExecutionId(request.GoldenExecutionId),
            rules,
            request.Description,
            request.WorkflowId
        );

        var createdGate = await _gateRepository.CreateAsync(gate, cancellationToken);

        _logger.LogInformation("User {UserId} created regression gate {GateId}: {GateName}",
            userId, createdGate.Id, createdGate.Name);

        return Ok(MapToResponseDto(createdGate));
    }

    /// <summary>
    /// Lists regression gates for the current user.
    /// Optionally filter by workflow ID.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<RegressionGateResponseDto>>> ListGates(
        [FromQuery] string? workflowId,
        CancellationToken cancellationToken)
    {
        var userId = _userContext.CurrentUserId;

        IReadOnlyList<RegressionGate> gates;

        if (!string.IsNullOrWhiteSpace(workflowId))
        {
            gates = await _gateRepository.ListByWorkflowAsync(workflowId, cancellationToken);
            // Filter by user ownership
            gates = gates.Where(g => g.OwnerId == userId).ToList();
        }
        else
        {
            gates = await _gateRepository.ListByUserAsync(userId, cancellationToken);
        }

        var response = gates.Select(MapToResponseDto).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Gets a specific regression gate by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RegressionGateResponseDto>> GetGate(
        string id,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var gateGuid))
        {
            return BadRequest(new { message = "Invalid gate ID format" });
        }

        var gate = await _gateRepository.GetByIdAsync(new RegressionGateId(gateGuid), cancellationToken);

        if (gate == null)
        {
            return NotFound(new { message = $"Regression gate {id} not found" });
        }

        var userId = _userContext.CurrentUserId;
        if (gate.OwnerId != userId)
        {
            return Forbid();
        }

        return Ok(MapToResponseDto(gate));
    }

    /// <summary>
    /// Deletes a regression gate.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGate(
        string id,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var gateGuid))
        {
            return BadRequest(new { message = "Invalid gate ID format" });
        }

        var gateId = new RegressionGateId(gateGuid);

        // Verify ownership
        var gate = await _gateRepository.GetByIdAsync(gateId, cancellationToken);
        if (gate == null)
        {
            return NotFound(new { message = $"Regression gate {id} not found" });
        }

        var userId = _userContext.CurrentUserId;
        if (gate.OwnerId != userId)
        {
            return Forbid();
        }

        var deleted = await _gateRepository.DeleteAsync(gateId, cancellationToken);

        if (!deleted)
        {
            return NotFound(new { message = $"Regression gate {id} not found" });
        }

        _logger.LogInformation("User {UserId} deleted regression gate {GateId}: {GateName}",
            userId, gateId, gate.Name);

        return NoContent();
    }

    /// <summary>
    /// Toggles the active status of a regression gate.
    /// </summary>
    [HttpPatch("{id}/toggle")]
    public async Task<ActionResult<RegressionGateResponseDto>> ToggleGate(
        string id,
        [FromBody] ToggleRegressionGateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var gateGuid))
        {
            return BadRequest(new { message = "Invalid gate ID format" });
        }

        var gateId = new RegressionGateId(gateGuid);

        // Verify ownership
        var gate = await _gateRepository.GetByIdAsync(gateId, cancellationToken);
        if (gate == null)
        {
            return NotFound(new { message = $"Regression gate {id} not found" });
        }

        var userId = _userContext.CurrentUserId;
        if (gate.OwnerId != userId)
        {
            return Forbid();
        }

        var updated = await _gateRepository.UpdateIsActiveAsync(gateId, request.IsActive, cancellationToken);

        if (!updated)
        {
            return NotFound(new { message = $"Regression gate {id} not found" });
        }

        // Re-fetch to get updated state
        var updatedGate = await _gateRepository.GetByIdAsync(gateId, cancellationToken);

        _logger.LogInformation("User {UserId} toggled regression gate {GateId} to {IsActive}",
            userId, gateId, request.IsActive);

        return Ok(MapToResponseDto(updatedGate!));
    }

    /// <summary>
    /// Runs a regression gate test against a candidate execution.
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<ActionResult<RegressionTestResultDto>> RunGateTest(
        string id,
        [FromBody] RunGateTestRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var gateGuid))
        {
            return BadRequest(new { message = "Invalid gate ID format" });
        }

        var gateId = new RegressionGateId(gateGuid);

        // Verify ownership
        var gate = await _gateRepository.GetByIdAsync(gateId, cancellationToken);
        if (gate == null)
        {
            return NotFound(new { message = $"Regression gate {id} not found" });
        }

        var userId = _userContext.CurrentUserId;
        if (gate.OwnerId != userId)
        {
            return Forbid();
        }

        // Verify candidate execution exists and is owned by user
        var candidateExecution = await _executionStore.GetAsync(request.CandidateExecutionId);
        if (candidateExecution == null)
        {
            return NotFound(new { message = $"Candidate execution {request.CandidateExecutionId} not found" });
        }

        if (candidateExecution.UserId != userId)
        {
            return Forbid();
        }

        try
        {
            var result = await _gateService.RunGateAsync(gateId, request.CandidateExecutionId, cancellationToken);

            _logger.LogInformation(
                "User {UserId} ran regression gate {GateId}. Result: {Passed}. Similarity: {Similarity:P2}",
                userId, gateId, result.Passed, result.DivergenceSummary.SimilarityPercentage);

            return Ok(MapToTestResultDto(result));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to run regression gate {GateId}", gateId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Marks an execution as golden (baseline).
    /// </summary>
    [HttpPost("/api/executions/{executionId}/mark-golden")]
    public async Task<IActionResult> MarkAsGolden(
        string executionId,
        [FromBody] MarkGoldenRequestDto request,
        CancellationToken cancellationToken)
    {
        // Verify execution exists and is owned by user
        var execution = await _executionStore.GetAsync(executionId);
        if (execution == null)
        {
            return NotFound(new { message = $"Execution {executionId} not found" });
        }

        var userId = _userContext.CurrentUserId;
        if (execution.UserId != userId)
        {
            return Forbid();
        }

        await _goldenStore.MarkAsGoldenAsync(executionId, request.Label, cancellationToken);

        _logger.LogInformation("User {UserId} marked execution {ExecutionId} as golden with label: {Label}",
            userId, executionId, request.Label ?? "(none)");

        return Ok(new { message = "Execution marked as golden", executionId, label = request.Label });
    }

    /// <summary>
    /// Unmarks an execution as golden.
    /// </summary>
    [HttpDelete("/api/executions/{executionId}/mark-golden")]
    public async Task<IActionResult> UnmarkAsGolden(
        string executionId,
        CancellationToken cancellationToken)
    {
        // Verify execution exists and is owned by user
        var execution = await _executionStore.GetAsync(executionId);
        if (execution == null)
        {
            return NotFound(new { message = $"Execution {executionId} not found" });
        }

        var userId = _userContext.CurrentUserId;
        if (execution.UserId != userId)
        {
            return Forbid();
        }

        var unmarked = await _goldenStore.UnmarkAsGoldenAsync(executionId, cancellationToken);

        if (!unmarked)
        {
            return NotFound(new { message = $"Execution {executionId} is not marked as golden" });
        }

        _logger.LogInformation("User {UserId} unmarked execution {ExecutionId} as golden", userId, executionId);

        return NoContent();
    }

    /// <summary>
    /// Lists all golden executions for the current user.
    /// </summary>
    [HttpGet("/api/executions/golden")]
    public async Task<ActionResult<List<GoldenExecutionMetadataDto>>> ListGoldenExecutions(
        CancellationToken cancellationToken)
    {
        var userId = _userContext.CurrentUserId;

        var goldenExecutions = await _goldenStore.ListByUserAsync(userId, cancellationToken);

        var response = goldenExecutions.Select(g => new GoldenExecutionMetadataDto
        {
            ExecutionId = g.ExecutionId,
            Label = g.Label,
            MarkedAt = g.MarkedAtUtc
        }).ToList();

        return Ok(response);
    }

    private static RegressionGateResponseDto MapToResponseDto(RegressionGate gate)
    {
        return new RegressionGateResponseDto
        {
            Id = gate.Id.Value.ToString(),
            Name = gate.Name,
            Description = gate.Description,
            WorkflowId = gate.WorkflowId,
            GoldenExecutionId = gate.GoldenExecutionId.Value,
            Rules = gate.Rules.Select(r => new DivergenceRuleDto
            {
                Type = r.Type.ToStringValue(),
                Threshold = r.Threshold
            }).ToList(),
            IsActive = gate.IsActive,
            CreatedAt = gate.CreatedAtUtc
        };
    }

    private static RegressionTestResultDto MapToTestResultDto(RegressionTestResult result)
    {
        return new RegressionTestResultDto
        {
            GateId = result.GateId.Value.ToString(),
            GateName = result.GateName,
            CandidateExecutionId = result.CandidateExecutionId,
            GoldenExecutionId = result.GoldenExecutionId.Value,
            Passed = result.Passed,
            RuleResults = result.RuleResults.Select(r => new RuleEvaluationResultDto
            {
                RuleType = r.RuleType.ToStringValue(),
                Passed = r.Passed,
                ActualValue = r.ActualValue,
                Threshold = r.Threshold,
                Reason = r.Reason
            }).ToList(),
            DivergenceSummary = new DivergenceSummaryDto
            {
                SimilarityPercentage = result.DivergenceSummary.SimilarityPercentage,
                IdenticalTaskCount = result.DivergenceSummary.IdenticalTaskCount,
                DifferentTaskCount = result.DivergenceSummary.DifferentTaskCount,
                DivergencePointIndex = result.DivergenceSummary.DivergencePointIndex,
                CriticalPathTaskIds = result.DivergenceSummary.CriticalPathTaskIds.ToList()
            },
            TestedAt = result.TestedAtUtc
        };
    }
}