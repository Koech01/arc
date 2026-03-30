using Arc.Api.DTOs.Execution;
using Arc.Application.Results;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Identity;
using Arc.Application.Execution;
using Arc.Application.Telemetry;
using Arc.Application.Workflows;
using Microsoft.AspNetCore.Authorization;


namespace Arc.Api.Controllers;
/// <summary>
/// Execution query and filtering endpoint.
/// Allows clients to list, filter, and analyze stored executions with aggregated analytics.
/// </summary>
[ApiController]
[Route("api/executions")]
[Authorize]
public sealed class ExecutionsController : ControllerBase
{
    private readonly IExecutionResultStore _resultStore;
    private readonly IAuditLogger _auditLogger;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly IUserContext _userContext;

    public ExecutionsController(
        IExecutionResultStore resultStore,
        IAuditLogger auditLogger,
        IWorkflowRepository workflowRepository,
        IWorkflowExecutor workflowExecutor,
        IUserContext userContext)
    {
        _resultStore = resultStore ?? throw new ArgumentNullException(nameof(resultStore));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        _workflowExecutor = workflowExecutor ?? throw new ArgumentNullException(nameof(workflowExecutor));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    /// <summary>
    /// Lists stored executions with filtering and pagination.
    /// The simple format (default) includes workflowName, workflowDescription, and tasks
    /// so the frontend can render all executions regardless of origin.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListExecutions(
        string? status = null,
        string? startDate = null,
        string? endDate = null,
        int? minTaskCount = null,
        int? maxTaskCount = null,
        int? minExecutionTimeMs = null,
        int? maxExecutionTimeMs = null,
        int? limit = null,
        int? offset = null,
        bool simple = true,
        bool includeArchived = false)
    {
        try
        {
            DateTime? parsedStartDate = null;
            DateTime? parsedEndDate = null;

            if (!string.IsNullOrWhiteSpace(startDate))
            {
                if (!DateTime.TryParse(startDate, out var parsed))
                    return BadRequest(new { message = $"Invalid startDate format: '{startDate}'. Use ISO 8601 format.", code = "BAD_REQUEST" });
                parsedStartDate = parsed.ToUniversalTime();
            }

            if (!string.IsNullOrWhiteSpace(endDate))
            {
                if (!DateTime.TryParse(endDate, out var parsed))
                    return BadRequest(new { message = $"Invalid endDate format: '{endDate}'. Use ISO 8601 format.", code = "BAD_REQUEST" });
                parsedEndDate = parsed.ToUniversalTime();
            }

            if (parsedStartDate.HasValue && parsedEndDate.HasValue && parsedStartDate > parsedEndDate)
                return BadRequest(new { message = "startDate must not be after endDate.", code = "BAD_REQUEST" });

            if (minTaskCount.HasValue && maxTaskCount.HasValue && minTaskCount > maxTaskCount)
                return BadRequest(new { message = "minTaskCount must not be greater than maxTaskCount.", code = "BAD_REQUEST" });

            if (minExecutionTimeMs.HasValue && maxExecutionTimeMs.HasValue && minExecutionTimeMs > maxExecutionTimeMs)
                return BadRequest(new { message = "minExecutionTimeMs must not be greater than maxExecutionTimeMs.", code = "BAD_REQUEST" });

            var filter = new ExecutionQueryFilter(
                Status: status,
                StartDateUtc: parsedStartDate,
                EndDateUtc: parsedEndDate,
                MinTaskCount: minTaskCount,
                MaxTaskCount: maxTaskCount,
                MinAverageExecutionTimeMs: minExecutionTimeMs,
                MaxAverageExecutionTimeMs: maxExecutionTimeMs,
                IncludeArchived: includeArchived);

            var pagination = PaginationParams.Validate(limit, offset);
            var currentUserId = _userContext.CurrentUserId;
            var queryResult = await _resultStore.QueryAsync(filter, pagination, currentUserId.Value);

            if (simple)
            {
                var simpleResponse = new List<ExecutionListItemResponseDto>();

                foreach (var exec in queryResult.Executions)
                {
                    var result = await _resultStore.GetAsync(exec.ExecutionId);
                    if (result is null)
                        continue;

                    var logs = await _auditLogger.GetExecutionLogsAsync(exec.ExecutionId);
                    var startLog = logs.FirstOrDefault(l => l.EventType == AuditEventType.OrchestratorStarted);
                    var endLog = logs.FirstOrDefault(l => l.EventType == AuditEventType.OrchestratorFinished);

                    var startedAt = startLog?.TimestampUtc ?? exec.CreatedAtUtc;
                    var duration = endLog != null && startLog != null
                        ? endLog.TimestampUtc - startLog.TimestampUtc
                        : TimeSpan.Zero;

                    var durationStr = duration.TotalMinutes >= 1
                        ? $"{(int)duration.TotalMinutes}m {duration.Seconds}s"
                        : $"{duration.Seconds}s";

                    var execStatus = result.Tasks.All(t => t.Status == TaskExecutionStatus.Succeeded)
                        ? "completed"
                        : result.Tasks.Any(t => t.Status == TaskExecutionStatus.Failed)
                            ? "failed"
                            : "running";

                    var tasks = result.Tasks
                        .OrderBy(t => t.ExecutionOrder)
                        .Select(t => new TaskSummaryDto
                        {
                            TaskId = t.TaskId,
                            TaskName = t.TaskName,
                            ExecutionOrder = t.ExecutionOrder,
                            Status = t.Status switch
                            {
                                TaskExecutionStatus.Succeeded => "success",
                                TaskExecutionStatus.Failed    => "failed",
                                _                             => "running"
                            },
                            Output = t.Output
                        })
                        .ToList();

                    simpleResponse.Add(new ExecutionListItemResponseDto
                    {
                        Id = exec.ExecutionId,
                        Status = execStatus,
                        TotalTasks = exec.TaskCount,
                        Duration = durationStr,
                        StartedAt = startedAt,
                        WorkflowName = exec.WorkflowName,
                        WorkflowDescription = exec.WorkflowDescription,
                        Tasks = tasks,
                        IsArchived = exec.IsArchived
                    });
                }

                return Ok(simpleResponse);
            }
            else
            {
                var totalPages = (queryResult.TotalAvailable + pagination.Limit - 1) / pagination.Limit;

                var response = new ExecutionListResponseDto(
                    Executions: queryResult.Executions
                        .Select(e => new ExecutionListItemDto(
                            ExecutionId: e.ExecutionId,
                            CreatedAtUtc: e.CreatedAtUtc,
                            TaskCount: e.TaskCount,
                            AverageExecutionTimeMs: e.AverageExecutionTimeMs,
                            Status: e.Status,
                            WorkflowName: e.WorkflowName,
                            WorkflowDescription: e.WorkflowDescription,
                            IsArchived: e.IsArchived))
                        .ToList(),
                    Analytics: new ExecutionAnalyticsDto(
                        TotalCount: queryResult.Analytics.TotalCount,
                        SuccessCount: queryResult.Analytics.SuccessCount,
                        FailureCount: queryResult.Analytics.FailureCount,
                        SuccessRate: queryResult.Analytics.SuccessRate,
                        AverageTaskCount: queryResult.Analytics.AverageTaskCount,
                        AverageExecutionTimeMs: queryResult.Analytics.AverageExecutionTimeMs),
                    Limit: pagination.Limit,
                    Offset: pagination.Offset,
                    TotalAvailable: queryResult.TotalAvailable,
                    TotalPages: totalPages);

                return Ok(response);
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message, code = "BAD_REQUEST" });
        }
    }

    /// <summary>
    /// Get detailed execution information by ID.
    /// Includes workflowName, workflowDescription, and task list.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ExecutionDetailDto>> GetExecution(string id)
    {
        try
        {
            var result = await _resultStore.GetAsync(id);
            if (result is null)
                return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });

            var currentUserId = _userContext.CurrentUserId;
            if (result.UserId.Value != currentUserId.Value)
                return StatusCode(403, new { message = "You do not have permission to access this resource.", code = "FORBIDDEN" });

            var workflowContext = await _resultStore.GetWorkflowContextAsync(id);

            var logs = await _auditLogger.GetExecutionLogsAsync(id);
            var startLog = logs.FirstOrDefault(l => l.EventType == AuditEventType.OrchestratorStarted);
            var endLog = logs.FirstOrDefault(l => l.EventType == AuditEventType.OrchestratorFinished);

            var startedAt = startLog?.TimestampUtc ?? DateTime.UtcNow;
            var completedAt = endLog?.TimestampUtc;
            var duration = completedAt.HasValue
                ? (long)(completedAt.Value - startedAt).TotalMilliseconds
                : 0;

            var status = result.Tasks.All(t => t.Status == TaskExecutionStatus.Succeeded)
                ? "success"
                : result.Tasks.Any(t => t.Status == TaskExecutionStatus.Failed)
                    ? "failed"
                    : "running";

            var tasks = result.Tasks
                .OrderBy(t => t.ExecutionOrder)
                .Select(t => new TaskSummaryDto
                {
                    TaskId = t.TaskId,
                    TaskName = t.TaskName,
                    ExecutionOrder = t.ExecutionOrder,
                    Status = t.Status switch
                    {
                        TaskExecutionStatus.Succeeded => "success",
                        TaskExecutionStatus.Failed    => "failed",
                        _                             => "running"
                    },
                    Output = t.Output
                })
                .ToList();

            var response = new ExecutionDetailDto
            {
                Id = id,
                Status = status,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                Duration = duration,
                TriggerType = "Manual",
                WorkflowId = workflowContext?.WorkflowId,
                WorkflowName = workflowContext?.WorkflowName ?? string.Empty,
                WorkflowDescription = workflowContext?.WorkflowDescription ?? string.Empty,
                Tasks = tasks
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", code = "INTERNAL_ERROR", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all tasks for a specific execution.
    /// </summary>
    [HttpGet("{id}/tasks")]
    public async Task<ActionResult<List<ExecutionTaskDto>>> GetExecutionTasks(string id)
    {
        var result = await _resultStore.GetAsync(id);
        if (result is null)
            return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });

        var currentUserId = _userContext.CurrentUserId;
        if (result.UserId.Value != currentUserId.Value)
            return StatusCode(403, new { message = "You do not have permission to access this resource.", code = "FORBIDDEN" });

        var logs = await _auditLogger.GetExecutionLogsAsync(id);

        var tasks = result.Tasks.Select(t =>
        {
            var taskStartLog = logs.FirstOrDefault(l =>
                l.EventType == AuditEventType.TaskStarted && l.TaskId == t.TaskId);
            var taskEndLog = logs.FirstOrDefault(l =>
                l.EventType == AuditEventType.TaskFinished && l.TaskId == t.TaskId);

            var tStartedAt = taskStartLog?.TimestampUtc;
            var tCompletedAt = taskEndLog?.TimestampUtc;
            var tDuration = tStartedAt.HasValue && tCompletedAt.HasValue
                ? (long)(tCompletedAt.Value - tStartedAt.Value).TotalMilliseconds
                : 0;

            var tStatus = t.Status switch
            {
                TaskExecutionStatus.Succeeded => "success",
                TaskExecutionStatus.Failed    => "failed",
                _                             => "running"
            };

            return new ExecutionTaskDto
            {
                Id = t.TaskId,
                Name = t.TaskName,
                Status = tStatus,
                StartedAt = tStartedAt,
                CompletedAt = tCompletedAt,
                Duration = tDuration,
                AgentType = "HTTP Agent",
                Dependencies = new List<string>(),
                Output = t.Output,
                Error = t.Status == TaskExecutionStatus.Failed ? "Task execution failed" : null
            };
        }).ToList();

        return Ok(tasks);
    }

    /// <summary>
    /// Get execution logs.
    /// </summary>
    [HttpGet("{id}/logs")]
    public async Task<ActionResult<List<ExecutionLogDto>>> GetExecutionLogs(string id)
    {
        var result = await _resultStore.GetAsync(id);
        if (result is null)
            return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });

        var currentUserId = _userContext.CurrentUserId;
        if (result.UserId.Value != currentUserId.Value)
            return StatusCode(403, new { message = "You do not have permission to access this resource.", code = "FORBIDDEN" });

        var logs = await _auditLogger.GetExecutionLogsAsync(id);

        var logDtos = logs.Select(l => new ExecutionLogDto
        {
            Id = $"log-{l.Sequence}",
            Timestamp = l.TimestampUtc,
            Level = l.EventType == AuditEventType.TaskFinished && l.Message?.Contains("Failed") == true
                ? "error"
                : "info",
            Message = l.Message ?? l.EventType.ToString(),
            TaskId = l.TaskId
        }).ToList();

        return Ok(logDtos);
    }

    /// <summary>
    /// Get execution outputs/results.
    /// </summary>
    [HttpGet("{id}/outputs")]
    public async Task<ActionResult<List<ExecutionOutputDto>>> GetExecutionOutputs(string id)
    {
        var result = await _resultStore.GetAsync(id);
        if (result is null)
            return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });

        var currentUserId = _userContext.CurrentUserId;
        if (result.UserId.Value != currentUserId.Value)
            return StatusCode(403, new { message = "You do not have permission to access this resource.", code = "FORBIDDEN" });

        var outputs = new List<ExecutionOutputDto>();

        var successfulTasks = result.Tasks.Where(t => t.Status == TaskExecutionStatus.Succeeded).ToList();
        if (successfulTasks.Any())
        {
            var finalTask = successfulTasks.OrderByDescending(t => t.ExecutionOrder).First();
            outputs.Add(new ExecutionOutputDto
            {
                Key = "final_result",
                Value = finalTask.Output,
                Type = "application/json"
            });

            var summary = $"Processing completed. {successfulTasks.Count} tasks succeeded, {result.Tasks.Count - successfulTasks.Count} tasks failed.";
            outputs.Add(new ExecutionOutputDto
            {
                Key = "summary_report",
                Value = summary,
                Type = "text/plain"
            });
        }

        return Ok(outputs);
    }

    /// <summary>
    /// Get execution metadata.
    /// </summary>
    [HttpGet("{id}/metadata")]
    public async Task<ActionResult<ExecutionMetadataDto>> GetExecutionMetadata(string id)
    {
        var result = await _resultStore.GetAsync(id);
        if (result is null)
            return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });

        var currentUserId = _userContext.CurrentUserId;
        if (result.UserId.Value != currentUserId.Value)
            return StatusCode(403, new { message = "You do not have permission to access this resource.", code = "FORBIDDEN" });

        var workflowContext = await _resultStore.GetWorkflowContextAsync(id);

        var metadata = new ExecutionMetadataDto
        {
            ExecutionId = id,
            WorkflowId = workflowContext?.WorkflowId,
            WorkflowVersion = "1.0.0",
            TriggeredBy = result.UserId.Value.ToString(),
            Environment = "production",
            TotalTasks = result.Tasks.Count,
            SuccessfulTasks = result.Tasks.Count(t => t.Status == TaskExecutionStatus.Succeeded),
            FailedTasks = result.Tasks.Count(t => t.Status == TaskExecutionStatus.Failed)
        };

        return Ok(metadata);
    }

    /// <summary>
    /// Archive an execution (soft delete).
    /// </summary>
    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchiveExecution(string id, [FromBody] ArchiveExecutionRequest? request)
    {
        try
        {
            var result = await _resultStore.GetAsync(id);
            if (result is null)
                return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });

            var userId = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized(new { message = "Invalid user credentials.", code = "UNAUTHORIZED" });

            if (result.UserId.Value != userGuid)
                return StatusCode(403, new { message = "You do not have permission to access this resource.", code = "FORBIDDEN" });

            await _resultStore.ArchiveAsync(id, userGuid, request?.Reason, request?.RetentionDays);
            return Ok(new { message = "Execution archived successfully", executionId = id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to archive execution", code = "INTERNAL_ERROR", details = ex.Message });
        }
    }

    /// <summary>
    /// Unarchive an execution.
    /// </summary>
    [HttpPost("{id}/unarchive")]
    public async Task<IActionResult> UnarchiveExecution(string id)
    {
        try
        {
            var result = await _resultStore.GetAsync(id);
            if (result is null)
                return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });

            var userId = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized(new { message = "Invalid user credentials.", code = "UNAUTHORIZED" });

            if (result.UserId.Value != userGuid)
                return StatusCode(403, new { message = "You do not have permission to access this resource.", code = "FORBIDDEN" });

            await _resultStore.UnarchiveAsync(id, userGuid);
            return Ok(new { message = "Execution unarchived successfully", executionId = id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to unarchive execution", code = "INTERNAL_ERROR", details = ex.Message });
        }
    }

    /// <summary>
    /// Permanently delete an execution (hard delete). Requires admin role.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PurgeExecution(string id, [FromBody] PurgeExecutionRequest? request)
    {
        try
        {
            var result = await _resultStore.GetAsync(id);
            if (result is null)
                return NotFound(new { message = "The requested resource was not found.", code = "NOT_FOUND" });

            var userId = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized(new { message = "Invalid user credentials.", code = "UNAUTHORIZED" });

            await _resultStore.PurgeAsync(id, userGuid, request?.Reason);
            return Ok(new { message = "Execution permanently deleted", executionId = id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to purge execution", code = "INTERNAL_ERROR", details = ex.Message });
        }
    }

    /// <summary>
    /// Get archive audit trail for an execution.
    /// </summary>
    [HttpGet("{id}/archive-audit")]
    public async Task<ActionResult<List<ArchiveAuditDto>>> GetArchiveAudit(string id)
    {
        try
        {
            var audit = await _resultStore.GetArchiveAuditAsync(id);
            var dtos = audit.Select(a => new ArchiveAuditDto
            {
                Id = a.Id,
                ExecutionId = a.ExecutionId,
                Action = a.Action,
                PerformedBy = a.PerformedBy.ToString(),
                PerformedAt = a.PerformedAtUtc,
                Reason = a.Reason
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve archive audit", code = "INTERNAL_ERROR", details = ex.Message });
        }
    }

    /// <summary>
    /// Replays an execution by re-running its original workflow.
    /// Creates a new execution with a new deterministic ID.
    /// </summary>
    [HttpPost("{id}/replay")]
    public async Task<IActionResult> ReplayExecution(string id)
    {
        try
        {
            // Get original execution
            var execution = await _resultStore.GetAsync(id);
            if (execution is null)
                return NotFound(new { message = "Execution not found", code = "NOT_FOUND" });

            // Get workflow context to find the workflow ID
            var workflowContext = await _resultStore.GetWorkflowContextAsync(id);
            if (workflowContext is null || string.IsNullOrEmpty(workflowContext.WorkflowId))
            {
                return BadRequest(new { message = "Cannot replay execution: No associated workflow found. Only executions from workflows can be replayed.", code = "NO_WORKFLOW" });
            }

            // Get the workflow
            var workflow = await _workflowRepository.GetByIdAsync(workflowContext.WorkflowId);
            if (workflow is null)
            {
                return NotFound(new { message = "Associated workflow not found", code = "WORKFLOW_NOT_FOUND" });
            }

            // Verify ownership
            var currentUserId = _userContext.CurrentUserId;
            if (execution.UserId.Value != currentUserId.Value)
            {
                return StatusCode(403, new { message = "You do not have permission to replay this execution", code = "FORBIDDEN" });
            }

            if (workflow.CreatedBy.Value != currentUserId.Value)
            {
                return StatusCode(403, new { message = "You do not have permission to execute this workflow", code = "FORBIDDEN" });
            }

            // Execute the workflow
            var result = _workflowExecutor.Execute(workflow);
            var newExecutionId = result.ExecutionId;

            // Store the new execution with workflow context
            var newWorkflowContext = new ExecutionWorkflowContext(
                WorkflowId: workflow.Id,
                WorkflowName: workflow.Name,
                WorkflowDescription: workflow.Description ?? string.Empty);

            await _resultStore.StoreAsync(newExecutionId, result, newWorkflowContext);

            return Ok(new { executionId = newExecutionId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to replay execution", code = "INTERNAL_ERROR", details = ex.Message });
        }
    }
}

public sealed record ArchiveExecutionRequest(string? Reason, int? RetentionDays);
public sealed record PurgeExecutionRequest(string? Reason);
public sealed record ArchiveAuditDto
{
    public long Id { get; init; }
    public string ExecutionId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string PerformedBy { get; init; } = string.Empty;
    public DateTime PerformedAt { get; init; }
    public string? Reason { get; init; }
}