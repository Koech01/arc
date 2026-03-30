using Arc.Api.DTOs.LLM;
using Arc.Domain.Models;
using System.Diagnostics;
using Arc.Application.LLM;
using Arc.Infrastructure.LLM;
using Arc.Application.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace Arc.Api.Controllers;
[ApiController]
[Route("api/llm-configs")]
[Authorize]
public sealed class LLMConfigsController : ControllerBase
{
    private readonly ILLMConfigurationRepository _repository;
    private readonly IUserContext _userContext;
    private readonly LLMProviderFactory _providerFactory;
    private readonly ILogger<LLMConfigsController> _logger;

    public LLMConfigsController(
        ILLMConfigurationRepository repository,
        IUserContext userContext,
        LLMProviderFactory providerFactory,
        ILogger<LLMConfigsController> logger)
    {
        _repository = repository;
        _userContext = userContext;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<LLMConfigResponseDto>>> List()
    {
        var userId = _userContext.CurrentUserId;
        if (userId == UserId.Anonymous)
            return Unauthorized(new { message = "Authentication required" });

        var configs = await _repository.ListByUserAsync(userId);
        var response = configs.Select(c => new LLMConfigResponseDto(
            c.Id, c.Name, c.BaseUrl, c.Model, c.Endpoint, c.AuthType, c.IsActive, c.CreatedAt
        )).ToList();

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LLMConfigResponseDto>> Get(string id)
    {
        var userId = _userContext.CurrentUserId;
        if (userId == UserId.Anonymous)
            return Unauthorized(new { message = "Authentication required" });

        var config = await _repository.GetByIdAsync(id, userId);
        if (config is null)
            return NotFound(new { message = "LLM configuration not found" });

        return Ok(new LLMConfigResponseDto(
            config.Id, config.Name, config.BaseUrl, config.Model, 
            config.Endpoint, config.AuthType, config.IsActive, config.CreatedAt
        ));
    }

    [HttpPost]
    public async Task<ActionResult<LLMConfigResponseDto>> Create([FromBody] CreateLLMConfigRequestDto request)
    {
        var userId = _userContext.CurrentUserId;
        if (userId == UserId.Anonymous)
            return Unauthorized(new { message = "Authentication required" });

        try
        {
            var config = LLMConfiguration.Create(
                request.Name,
                request.BaseUrl,
                request.Model,
                request.ApiKey,
                request.Endpoint,
                request.AuthType,
                request.Headers,
                userId
            );

            await _repository.CreateAsync(config);
            _logger.LogInformation("LLM config created: {Id} by user {UserId}", config.Id, userId);

            return CreatedAtAction(nameof(Get), new { id = config.Id }, 
                new LLMConfigResponseDto(config.Id, config.Name, config.BaseUrl, 
                    config.Model, config.Endpoint, config.AuthType, config.IsActive, config.CreatedAt));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/test")]
    public async Task<ActionResult<TestLLMConnectionResponseDto>> Test(string id)
    {
        var userId = _userContext.CurrentUserId;
        if (userId == UserId.Anonymous)
            return Unauthorized(new { message = "Authentication required" });

        var config = await _repository.GetByIdAsync(id, userId);
        if (config is null)
            return NotFound(new { message = "LLM configuration not found" });

        try
        {
            var sw = Stopwatch.StartNew();
            var provider = _providerFactory.CreateProvider(config);
            var result = await provider.GenerateTextAsync("Test");
            sw.Stop();

            return Ok(new TestLLMConnectionResponseDto(
                true, (int)sw.ElapsedMilliseconds, "Connection successful"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM test failed for config {Id}", id);
            return Ok(new TestLLMConnectionResponseDto(
                false, 0, $"Connection failed: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Partially updates an LLM configuration.
    /// PATCH /api/llm-configs/{id}
    /// Fields absent or null in the request body are preserved unchanged.
    /// An empty apiKey value keeps the stored key; supply a non-empty value to rotate it.
    /// Name uniqueness is enforced per user (excluding the current record).
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<ActionResult<LLMConfigResponseDto>> Patch(
        string id,
        [FromBody] UpdateLLMConfigRequestDto request)
    {
        var userId = _userContext.CurrentUserId;
        if (userId == UserId.Anonymous)
            return Unauthorized(new { message = "Authentication required" });

        var existing = await _repository.GetByIdAsync(id, userId);
        if (existing is null)
            return NotFound(new { message = "LLM configuration not found" });

        // Enforce name uniqueness per user when the name is being changed.
        if (request.Name is not null &&
            !request.Name.Equals(existing.Name, StringComparison.OrdinalIgnoreCase))
        {
            var userConfigs = await _repository.ListByUserAsync(userId);
            if (userConfigs.Any(c => c.Id != id &&
                    c.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new
                {
                    message = $"An LLM configuration named '{request.Name}' already exists."
                });
            }
        }

        try
        {
            var updated = existing.WithUpdates(
                name: request.Name,
                baseUrl: request.BaseUrl,
                model: request.Model,
                apiKey: request.ApiKey,
                endpoint: request.Endpoint,
                authType: request.AuthType,
                headers: request.Headers);

            await _repository.UpdateAsync(updated);

            _logger.LogInformation(
                "LLM config patched: {Id} by user {UserId}. nameChanged={NameChanged} modelChanged={ModelChanged} baseUrlChanged={BaseUrlChanged} apiKeyRotated={ApiKeyRotated}",
                id, userId,
                request.Name is not null,
                request.Model is not null,
                request.BaseUrl is not null,
                !string.IsNullOrEmpty(request.ApiKey));

            return Ok(new LLMConfigResponseDto(
                updated.Id,
                updated.Name,
                updated.BaseUrl,
                updated.Model,
                updated.Endpoint,
                updated.AuthType,
                updated.IsActive,
                updated.CreatedAt,
                MaskApiKey(updated.ApiKey)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = _userContext.CurrentUserId;
        if (userId == UserId.Anonymous)
            return Unauthorized(new { message = "Authentication required" });

        await _repository.DeleteAsync(id, userId);
        _logger.LogInformation("LLM config deleted: {Id} by user {UserId}", id, userId);

        return NoContent();
    }

    /// <summary>
    /// Returns a masked representation of the API key for safe inclusion in responses.
    /// E.g. "sk-abc...xyz1234" → "sk-***1234". Returns null when key is absent.
    /// </summary>
    private static string? MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return null;

        return apiKey.Length <= 4
            ? "sk-***"
            : $"sk-***{apiKey[^4..]}";
    }
}