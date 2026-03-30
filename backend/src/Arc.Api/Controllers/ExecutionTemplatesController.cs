using System.Text;
using Arc.Domain.Models;
using Arc.Application.LLM;
using Arc.Api.DTOs.Execution;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;
using Arc.Application.Identity;
using Arc.Application.Workflows;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;


namespace Arc.Api.Controllers;
/// <summary>
/// Execution template management endpoint.
/// Allows clients to create, list, and instantiate execution templates.
/// </summary>
[ApiController]
[Route("api/execution-templates")]
[Authorize]
public sealed class ExecutionTemplatesController : ControllerBase
{
    private readonly IExecutionTemplateStore _templateStore;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILLMConfigurationRepository _llmConfigRepository;
    private readonly IUserContext _userContext;
    private readonly ILogger<ExecutionTemplatesController> _logger;

    public ExecutionTemplatesController(
        IExecutionTemplateStore templateStore,
        IWorkflowRepository workflowRepository,
        ILLMConfigurationRepository llmConfigRepository,
        IUserContext userContext,
        ILogger<ExecutionTemplatesController> logger)
    {
        _templateStore = templateStore ?? throw new ArgumentNullException(nameof(templateStore));
        _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        _llmConfigRepository = llmConfigRepository ?? throw new ArgumentNullException(nameof(llmConfigRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new execution template from workflow tasks.
    /// </summary>
    /// <param name="request">Template creation request.</param>
    /// <returns>
    /// 201 Created with template metadata if successful.
    /// 400 Bad Request if request is invalid or template already exists.
    /// </returns>
    [HttpPost]
    public async Task<ActionResult<ExecutionTemplateResponseDto>> CreateTemplate([FromBody] CreateExecutionTemplateRequestDto request)
    {
        if (request is null)
            return BadRequest("Request cannot be null.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Template name cannot be null or empty.");

        if (request.Tasks is null || request.Tasks.Count == 0)
            return BadRequest("Template must have at least one task.");

        if (string.IsNullOrWhiteSpace(request.TriggerType))
            return BadRequest("Template trigger type cannot be null or empty.");

        try
        {
            var tasks = request.Tasks.Select(t => new WorkflowTask(
                t.Id,
                t.Name,
                t.AgentType,
                t.Prompt,
                t.LLMConfigId,
                t.Config ?? new Dictionary<string, string>(),
                t.Dependencies ?? new List<string>()
            )).ToList();

            var userId = _userContext.CurrentUserId;
            var template = await _templateStore.CreateAsync(
                request.Name,
                request.Description ?? "",
                tasks,
                request.TriggerType,
                userId,
                request.LLMConfigId);

            _logger.LogInformation("Template created: {TemplateName}", template.Name);

            var response = new ExecutionTemplateResponseDto(
                Name: template.Name,
                Description: template.Description,
                CreatedAtUtc: template.CreatedAtUtc,
                UseCount: template.UseCount
            );

            return CreatedAtAction(nameof(GetTemplate), new { name = template.Name }, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template {TemplateName}", request.Name);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves a template by name with full details.
    /// </summary>
    /// <param name="name">Template name.</param>
    /// <returns>
    /// 200 OK with template details if found.
    /// 404 Not Found if template does not exist.
    /// </returns>
    [HttpGet("{name}")]
    public async Task<ActionResult<ExecutionTemplateDetailDto>> GetTemplate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Template name cannot be null or empty.");

        var template = await _templateStore.GetAsync(name, _userContext.CurrentUserId);
        if (template is null)
            return NotFound($"Template '{name}' not found.");

        var response = new ExecutionTemplateDetailDto(
            Name: template.Name,
            Description: template.Description,
            CreatedAtUtc: template.CreatedAtUtc,
            UseCount: template.UseCount,
            TriggerType: template.TriggerType,
            LLMConfigId: template.LLMConfigId,
            Tasks: template.Tasks.Select(t => new TemplateTaskDto(
                t.Id,
                t.Name,
                t.AgentType,
                t.Prompt,
                t.LLMConfigId,
                t.Config?.ToDictionary(kv => kv.Key, kv => kv.Value),
                t.Dependencies?.ToList()
            )).ToList()
        );

        return Ok(response);
    }

    /// <summary>
    /// Lists all execution templates.
    /// </summary>
    /// <returns>
    /// 200 OK with list of template metadata (ordered by name).
    /// </returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExecutionTemplateMetadataDto>>> ListTemplates()
    {
        var templates = await _templateStore.ListAsync(_userContext.CurrentUserId);

        var response = templates
            .Select(t => new ExecutionTemplateMetadataDto(
                Name: t.Name,
                Description: t.Description,
                CreatedAtUtc: t.CreatedAtUtc,
                UseCount: t.UseCount,
                LLMConfigId: t.LLMConfigId
            ))
            .ToList();

        return Ok(response);
    }

    /// <summary>
    /// Deletes a template by name.
    /// </summary>
    /// <param name="name">Template name.</param>
    /// <returns>
    /// 204 No Content if deleted successfully.
    /// 404 Not Found if template does not exist.
    /// </returns>
    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteTemplate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Template name cannot be null or empty.");

        var deleted = await _templateStore.DeleteAsync(name, _userContext.CurrentUserId);
        if (!deleted)
            return NotFound($"Template '{name}' not found.");

        return NoContent();
    }

    /// <summary>
    /// Instantiates a template with optional variable substitution and creates a workflow.
    /// </summary>
    /// <param name="name">Template name.</param>
    /// <param name="request">Instantiation request with variable substitution and workflow name.</param>
    /// <returns>
    /// 200 OK with workflow ID if template found and workflow created.
    /// 404 Not Found if template does not exist.
    /// 400 Bad Request if request is invalid.
    /// </returns>
    [HttpPost("{name}/instantiate")]
    public async Task<ActionResult<TemplateInstantiationResponseDto>> InstantiateTemplate(
        string name,
        [FromBody] TemplateInstantiationRequestDto? request = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Template name cannot be null or empty.");

        try
        {
            var result = await _templateStore.InstantiateAsync(name, _userContext.CurrentUserId, request?.Variables);
            if (result is null)
                return NotFound($"Template '{name}' not found.");

            // Request-level LLMConfigId overrides the template-level one.
            var resolvedLLMConfigId = request?.LLMConfigId ?? result.LLMConfigId;

            // Guard: every task must eventually resolve a config. If neither the request,
            // the template, nor any individual task carries one, fail before persisting.
            var allTasksHaveConfig = result.InstantiatedTasks.All(t => !string.IsNullOrWhiteSpace(t.LLMConfigId));
            if (string.IsNullOrWhiteSpace(resolvedLLMConfigId) && !allTasksHaveConfig)
            {
                return BadRequest(new
                {
                    message = "No LLM configuration is set on this template. " +
                              "Provide llmConfigId in the request body to specify which LLM configuration to use."
                });
            }

            var userId = _userContext.CurrentUserId;
            // Use the template's post-increment run number for a clean, readable name.
            var workflowName = $"{result.TemplateName}-run-{result.RunNumber}";
            var workflowId = GenerateWorkflowId(workflowName, userId);

            var workflow = new Workflow(
                workflowId,
                workflowName,
                $"Instantiated from template: {result.TemplateName}",
                result.InstantiatedTasks,
                result.TriggerType,
                userId,
                DateTime.UtcNow,
                resolvedLLMConfigId
            );

            await _workflowRepository.CreateAsync(workflow);

            _logger.LogInformation(
                "Template {TemplateName} instantiated as workflow {WorkflowId} by user {UserId}",
                result.TemplateName,
                workflowId,
                userId.Value);

            var response = new TemplateInstantiationResponseDto(workflowId, workflowName);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error instantiating template {TemplateName}", name);
            return StatusCode(500, "An error occurred while instantiating the template.");
        }
    }

    /// <summary>
    /// Full replacement of an existing execution template.
    /// PUT /api/execution-templates/{name}
    /// The name in the URL and request body must match.
    /// createdAt and useCount are preserved. All tasks are replaced atomically.
    /// llmConfigId, if provided, must exist and belong to the authenticated user.
    /// </summary>
    [HttpPut("{name}")]
    public async Task<ActionResult<ExecutionTemplateDetailDto>> PutTemplate(
        string name,
        [FromBody] CreateExecutionTemplateRequestDto request)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        // Name in URL must match name in body (case-insensitive).
        if (!string.Equals(name, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = $"Template name in URL ('{name}') does not match name in request body ('{request.Name}')."
            });
        }

        if (string.IsNullOrWhiteSpace(request.TriggerType))
            return BadRequest(new { message = "TriggerType is required." });

        if (request.Tasks is null || request.Tasks.Count == 0)
            return BadRequest(new { message = "Template must have at least one task." });

        var tasks = request.Tasks.Select(t => new WorkflowTask(
            t.Id,
            t.Name,
            t.AgentType,
            t.Prompt,
            t.LLMConfigId,
            t.Config ?? new Dictionary<string, string>(),
            t.Dependencies ?? new List<string>()
        )).ToList();

        if (HasCircularDependencies(tasks))
            return BadRequest(new { message = "Task dependencies contain a circular reference." });

        var userId = _userContext.CurrentUserId;

        // Validate that the supplied llmConfigId exists and belongs to the authenticated user.
        if (!string.IsNullOrWhiteSpace(request.LLMConfigId))
        {
            var llmConfig = await _llmConfigRepository.GetByIdAsync(request.LLMConfigId, userId);
            if (llmConfig is null)
            {
                return BadRequest(new
                {
                    message = $"LLM configuration '{request.LLMConfigId}' was not found or does not belong to you."
                });
            }
        }

        try
        {
            var updated = await _templateStore.UpdateAsync(
                name,
                request.Description ?? string.Empty,
                tasks,
                request.TriggerType,
                userId,
                request.LLMConfigId);

            if (!updated)
                return NotFound(new { message = $"Template '{name}' not found." });

            // Fetch the persisted state to include preserved fields (createdAt, useCount).
            var template = await _templateStore.GetAsync(name, userId);
            if (template is null)
                return NotFound(new { message = $"Template '{name}' not found after update." });

            _logger.LogInformation(
                "Template updated: {TemplateName} by user {UserId}. tasks={TaskCount} trigger={TriggerType}",
                name, userId.Value, tasks.Count, request.TriggerType);

            return Ok(new ExecutionTemplateDetailDto(
                Name: template.Name,
                Description: template.Description,
                CreatedAtUtc: template.CreatedAtUtc,
                UseCount: template.UseCount,
                TriggerType: template.TriggerType,
                LLMConfigId: template.LLMConfigId,
                Tasks: template.Tasks.Select(t => new TemplateTaskDto(
                    t.Id,
                    t.Name,
                    t.AgentType,
                    t.Prompt,
                    t.LLMConfigId,
                    t.Config?.ToDictionary(kv => kv.Key, kv => kv.Value),
                    t.Dependencies?.ToList()
                )).ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateName}", name);
            return StatusCode(500, new { message = "An error occurred while updating the template." });
        }
    }

    /// <summary>
    /// Detects circular task dependencies using iterative DFS.
    /// Returns true when at least one cycle is found.
    /// </summary>
    private static bool HasCircularDependencies(IReadOnlyList<WorkflowTask> tasks)
    {
        var taskIds = new HashSet<string>(tasks.Select(t => t.Id));
        var adjacency = tasks.ToDictionary(
            t => t.Id,
            t => (t.Dependencies ?? Enumerable.Empty<string>())
                    .Where(d => taskIds.Contains(d))
                    .ToList());

        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        bool HasCycle(string id)
        {
            if (inStack.Contains(id)) return true;
            if (visited.Contains(id)) return false;
            visited.Add(id);
            inStack.Add(id);
            foreach (var dep in adjacency.GetValueOrDefault(id, new List<string>()))
            {
                if (HasCycle(dep)) return true;
            }
            inStack.Remove(id);
            return false;
        }

        return tasks.Any(t => HasCycle(t.Id));
    }

    private static string GenerateWorkflowId(string name, UserId userId)
    {
        var input = $"{userId.Value}:{name}:{DateTime.UtcNow.Ticks}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return $"wf-{Convert.ToHexString(hash)[..16].ToLowerInvariant()}";
    }
}