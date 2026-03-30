using System.Text;
using Arc.Api.DTOs;
using FluentValidation;
using Arc.Domain.Models;
using Arc.Domain.Exceptions;
using Arc.Api.DTOs.Workflows;
namespace Arc.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Identity;
using Arc.Application.Workflows;
using Arc.Application.Execution;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;


[ApiController]
[Route("api/workflows")]
[Authorize]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUserContext _userContext;
    private readonly IValidator<CreateWorkflowRequestDto> _validator;
    private readonly ILogger<WorkflowsController> _logger;
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly IExecutionResultStore _executionResultStore;

    public WorkflowsController(
        IWorkflowRepository workflowRepository,
        IUserContext userContext,
        IValidator<CreateWorkflowRequestDto> validator,
        ILogger<WorkflowsController> logger,
        IWorkflowExecutor workflowExecutor,
        IExecutionResultStore executionResultStore)
    {
        _workflowRepository = workflowRepository;
        _userContext = userContext;
        _validator = validator;
        _logger = logger;
        _workflowExecutor = workflowExecutor;
        _executionResultStore = executionResultStore;
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowResponseDto>> CreateWorkflow(
        [FromBody] CreateWorkflowRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { message = "Validation failed", errors = validationResult.ToDictionary() });
        }

        var userId = _userContext.CurrentUserId;

        var existingWorkflow = await _workflowRepository.GetByNameAsync(request.Name, userId, cancellationToken);
        if (existingWorkflow != null)
        {
            return Conflict(new { message = $"Workflow with name '{request.Name}' already exists" });
        }

        try
        {
            var workflowId = GenerateWorkflowId(request.Name, userId);
            var tasks = request.Tasks.Select(t => new WorkflowTask(
                t.Id,
                t.Name,
                t.AgentType,
                t.Prompt,
                t.LLMConfigId,
                t.Config,
                t.Dependencies
            )).ToList();

            var workflow = new Workflow(
                workflowId,
                request.Name,
                request.Description,
                tasks,
                request.TriggerType,
                userId,
                DateTime.UtcNow,
                request.LLMConfigId
            );

            await _workflowRepository.CreateAsync(workflow, cancellationToken);

            _logger.LogInformation("Workflow created: {WorkflowId} by user {UserId}", workflowId, userId.Value);

            return CreatedAtAction(
                nameof(GetWorkflow),
                new { id = workflowId },
                new WorkflowResponseDto
                {
                    Id = workflow.Id,
                    Name = workflow.Name,
                    Description = workflow.Description,
                    CreatedAt = workflow.CreatedAt
                });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for workflow creation");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowDetailDto>> GetWorkflow(string id, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id, cancellationToken);
        if (workflow == null)
        {
            return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });
        }

        var userId = _userContext.CurrentUserId;
        if (workflow.CreatedBy.Value != userId.Value)
        {
            return StatusCode(403, new { message = "You do not have permission to access this resource.", code = "FORBIDDEN" });
        }

        return Ok(new WorkflowDetailDto
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Description = workflow.Description,
            Tasks = workflow.Tasks.Select(t => new WorkflowTaskDto
            {
                Id = t.Id,
                Name = t.Name,
                AgentType = t.AgentType,
                Prompt = t.Prompt,
                LLMConfigId = t.LLMConfigId,
                Config = new Dictionary<string, string>(t.Config),
                Dependencies = new List<string>(t.Dependencies)
            }).ToList(),
            TriggerType = workflow.TriggerType,
            CreatedBy = workflow.CreatedBy.Value.ToString(),
            CreatedAt = workflow.CreatedAt
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkflowListItemDto>>> ListWorkflows(CancellationToken cancellationToken)
    {
        var userId = _userContext.CurrentUserId;
        var workflows = await _workflowRepository.ListByUserAsync(userId, cancellationToken);

        var response = workflows.Select(w => new WorkflowListItemDto
        {
            Id = w.Id,
            Name = w.Name,
            Description = w.Description,
            TriggerType = w.TriggerType,
            CreatedAt = w.CreatedAt
        }).ToList();

        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkflow(string id, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id, cancellationToken);
        if (workflow == null)
        {
            return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });
        }

        var userId = _userContext.CurrentUserId;
        if (workflow.CreatedBy.Value != userId.Value)
        {
            return StatusCode(403, new { message = "You do not have permission to access this resource.", code = "FORBIDDEN" });
        }

        var deleted = await _workflowRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });
        }

        _logger.LogInformation("Workflow deleted: {WorkflowId} by user {UserId}", id, userId.Value);
        return NoContent();
    }

    [HttpPost("{id}/duplicate")]
    public async Task<ActionResult<WorkflowResponseDto>> DuplicateWorkflow(string id, CancellationToken cancellationToken)
    {
        // Get the source workflow
        var sourceWorkflow = await _workflowRepository.GetByIdAsync(id, cancellationToken);
        if (sourceWorkflow == null)
        {
            return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });
        }

        // Verify ownership
        var userId = _userContext.CurrentUserId;
        if (sourceWorkflow.CreatedBy.Value != userId.Value)
        {
            return StatusCode(403, new { message = "You do not have permission to access this resource.", code = "FORBIDDEN" });
        }

        // Generate a unique name for the duplicate
        var baseName = $"Copy of {sourceWorkflow.Name}";
        var duplicateName = baseName;
        var counter = 1;

        // Ensure the name is unique
        while (await _workflowRepository.GetByNameAsync(duplicateName, userId, cancellationToken) != null)
        {
            duplicateName = $"{baseName} ({counter})";
            counter++;
        }

        try
        {
            // Generate new workflow ID
            var newWorkflowId = GenerateWorkflowId(duplicateName, userId);

            // Copy all tasks with new IDs
            var duplicatedTasks = sourceWorkflow.Tasks.Select(t => new WorkflowTask(
                $"{t.Id}-copy-{DateTime.UtcNow.Ticks}",
                t.Name,
                t.AgentType,
                t.Prompt,
                t.LLMConfigId,
                new Dictionary<string, string>(t.Config),
                new List<string>(t.Dependencies)
            )).ToList();

            // Create the duplicate workflow
            var duplicateWorkflow = new Workflow(
                newWorkflowId,
                duplicateName,
                sourceWorkflow.Description,
                duplicatedTasks,
                sourceWorkflow.TriggerType,
                userId,
                DateTime.UtcNow,
                sourceWorkflow.LLMConfigId
            );

            await _workflowRepository.CreateAsync(duplicateWorkflow, cancellationToken);

            _logger.LogInformation(
                "Workflow duplicated: {SourceWorkflowId} -> {NewWorkflowId} by user {UserId}", 
                id, 
                newWorkflowId, 
                userId.Value);

            return CreatedAtAction(
                nameof(GetWorkflow),
                new { id = newWorkflowId },
                new WorkflowResponseDto
                {
                    Id = duplicateWorkflow.Id,
                    Name = duplicateWorkflow.Name,
                    Description = duplicateWorkflow.Description,
                    CreatedAt = duplicateWorkflow.CreatedAt
                });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for workflow duplication");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/execute")]
    public async Task<ActionResult<WorkflowExecutionResponseDto>> ExecuteWorkflow(string id)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id);
        if (workflow is null)
            return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });

        var currentUserId = _userContext.CurrentUserId;
        if (workflow.CreatedBy.Value != currentUserId.Value)
            return StatusCode(403, new { message = "You do not have permission to access this resource.", code = "FORBIDDEN" });

        var result = _workflowExecutor.Execute(workflow);
        var executionId = result.ExecutionId;

        // Attach workflow context so the UI can display workflowName and workflowDescription
        var workflowContext = new ExecutionWorkflowContext(
            WorkflowId: workflow.Id,
            WorkflowName: workflow.Name,
            WorkflowDescription: workflow.Description ?? string.Empty);

        await _executionResultStore.StoreAsync(executionId, result, workflowContext);

        var response = new WorkflowExecutionResponseDto
        {
            ExecutionId = executionId,
            WorkflowId = workflow.Id,
            WorkflowName = workflow.Name,
            Tasks = result.Tasks
                .OrderBy(t => t.ExecutionOrder)
                .Select(t => new TaskResultDto(
                    TaskId: t.TaskId,
                    TaskName: t.TaskName,
                    ExecutionOrder: t.ExecutionOrder,
                    Status: t.Status.ToString()))
                .ToArray()
        };

        return Ok(response);
    }

    private static string GenerateWorkflowId(string name, UserId userId)
    {
        var input = $"{userId.Value}:{name}:{DateTime.UtcNow.Ticks}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return $"wf-{Convert.ToHexString(hash)[..16].ToLowerInvariant()}";
    }
}