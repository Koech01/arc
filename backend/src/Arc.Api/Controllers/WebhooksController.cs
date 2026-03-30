using FluentValidation;
using Arc.Domain.Models;
using Arc.Api.DTOs.Webhooks;
using Arc.Domain.Exceptions;
namespace Arc.Api.Controllers;
using Arc.Application.Webhooks;
using Arc.Application.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


[ApiController]
[Route("api/webhooks")]
[Authorize]
public sealed class WebhooksController : ControllerBase
{
    private readonly IWebhookRepository _webhookRepository;
    private readonly IUserContext _userContext;
    private readonly IValidator<CreateWebhookRequestDto> _validator;
    private readonly IValidator<UpdateWebhookRequestDto> _updateValidator;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookRepository webhookRepository,
        IUserContext userContext,
        IValidator<CreateWebhookRequestDto> validator,
        IValidator<UpdateWebhookRequestDto> updateValidator,
        ILogger<WebhooksController> logger)
    {
        _webhookRepository = webhookRepository ?? throw new ArgumentNullException(nameof(webhookRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new webhook for execution events.
    /// POST /api/webhooks
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WebhookResponseDto>> RegisterWebhook(
        [FromBody] CreateWebhookRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { message = "Validation failed", errors = validationResult.ToDictionary() });
        }

        try
        {
            var userId = _userContext.CurrentUserId;
            var webhookId = WebhookId.Create();

            var eventTypes = request.Events
                .Select(e => WebhookEventTypeExtensions.FromEventString(e))
                .ToList();

            var webhook = new Webhook(
                webhookId,
                request.Url,
                eventTypes,
                request.Secret,
                isActive: true,
                userId,
                DateTime.UtcNow);

            await _webhookRepository.CreateAsync(webhook, cancellationToken);

            _logger.LogInformation("Webhook registered: {WebhookId} by user {UserId}", webhookId.Value, userId.Value);

            return CreatedAtAction(
                nameof(GetWebhook),
                new { id = webhookId.Value },
                MapToDto(webhook));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation error creating webhook");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating webhook");
            return StatusCode(500, new { message = "An error occurred while creating the webhook" });
        }
    }

    /// <summary>
    /// Retrieves a specific webhook by ID.
    /// GET /api/webhooks/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<WebhookResponseDto>> GetWebhook(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(id, out var webhookGuid))
                return BadRequest(new { message = "Invalid webhook ID format" });

            var webhook = await _webhookRepository.GetByIdAsync(WebhookId.From(webhookGuid), cancellationToken);
            if (webhook == null)
                return NotFound(new { message = "Webhook not found" });

            var userId = _userContext.CurrentUserId;
            if (webhook.CreatedBy.Value != userId.Value)
                return Forbid();

            return Ok(MapToDto(webhook));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook {WebhookId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the webhook" });
        }
    }

    /// <summary>
    /// Lists all webhooks for the authenticated user.
    /// GET /api/webhooks
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<WebhookListItemDto>>> ListWebhooks(
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _userContext.CurrentUserId;
            var webhooks = await _webhookRepository.ListByUserAsync(userId, cancellationToken);

            var dtos = webhooks.Select(MapToListItemDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing webhooks for user {UserId}", _userContext.CurrentUserId.Value);
            return StatusCode(500, new { message = "An error occurred while listing webhooks" });
        }
    }

    /// <summary>
    /// Deletes a webhook by ID.
    /// DELETE /api/webhooks/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebhook(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(id, out var webhookGuid))
                return BadRequest(new { message = "Invalid webhook ID format" });

            var webhookId = WebhookId.From(webhookGuid);
            var webhook = await _webhookRepository.GetByIdAsync(webhookId, cancellationToken);
            if (webhook == null)
                return NotFound(new { message = "Webhook not found" });

            var userId = _userContext.CurrentUserId;
            if (webhook.CreatedBy.Value != userId.Value)
                return Forbid();

            var deleted = await _webhookRepository.DeleteAsync(webhookId, cancellationToken);
            if (!deleted)
                return NotFound(new { message = "Webhook not found" });

            _logger.LogInformation("Webhook deleted: {WebhookId} by user {UserId}", webhookId.Value, userId.Value);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook {WebhookId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the webhook" });
        }
    }

    /// <summary>
    /// Toggles the active state of a webhook.
    /// PATCH /api/webhooks/{id}/toggle
    /// </summary>
    [HttpPatch("{id}/toggle")]
    public async Task<ActionResult<WebhookResponseDto>> ToggleWebhook(
        string id,
        [FromBody] ToggleWebhookRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(id, out var webhookGuid))
                return BadRequest(new { message = "Invalid webhook ID format" });

            var webhookId = WebhookId.From(webhookGuid);
            var webhook = await _webhookRepository.GetByIdAsync(webhookId, cancellationToken);
            if (webhook == null)
                return NotFound(new { message = "Webhook not found" });

            var userId = _userContext.CurrentUserId;
            if (webhook.CreatedBy.Value != userId.Value)
                return Forbid();

            await _webhookRepository.UpdateIsActiveAsync(webhookId, request.IsActive, cancellationToken);

            _logger.LogInformation("Webhook {WebhookId} toggled isActive={IsActive} by user {UserId}",
                webhookId.Value, request.IsActive, userId.Value);

            return Ok(new WebhookResponseDto
            {
                Id = webhook.Id.Value.ToString(),
                Url = webhook.Url,
                Events = webhook.Events.Select(e => e.ToEventString()).ToList(),
                IsActive = request.IsActive,
                CreatedAt = webhook.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling webhook {WebhookId}", id);
            return StatusCode(500, new { message = "An error occurred while toggling the webhook" });
        }
    }

    /// <summary>
    /// Partially updates a webhook's url, events, and optionally rotates its secret.
    /// PATCH /api/webhooks/{id}
    /// Secret is kept unchanged when absent or empty in the request body.
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<ActionResult<WebhookResponseDto>> PatchWebhook(
        string id,
        [FromBody] UpdateWebhookRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { message = "Validation failed", errors = validationResult.ToDictionary() });
        }

        try
        {
            if (!Guid.TryParse(id, out var webhookGuid))
                return BadRequest(new { message = "Invalid webhook ID format" });

            var webhookId = WebhookId.From(webhookGuid);
            var existing = await _webhookRepository.GetByIdAsync(webhookId, cancellationToken);
            if (existing == null)
                return NotFound(new { message = "Webhook not found" });

            var userId = _userContext.CurrentUserId;
            if (existing.CreatedBy.Value != userId.Value)
                return Forbid();

            var eventTypes = request.Events
                .Select(e => WebhookEventTypeExtensions.FromEventString(e))
                .ToList();

            // Preserve existing secret when the caller sends an absent or empty value.
            var secretToStore = !string.IsNullOrEmpty(request.Secret) ? request.Secret : existing.Secret;
            var secretRotated = !string.IsNullOrEmpty(request.Secret);

            var updated = new Webhook(
                webhookId,
                request.Url,
                eventTypes,
                secretToStore,
                existing.IsActive,
                existing.CreatedBy,
                existing.CreatedAt);

            await _webhookRepository.UpdateAsync(updated, cancellationToken);

            _logger.LogInformation(
                "Webhook updated: {WebhookId} by user {UserId}. url={Url} events={EventCount} secretRotated={SecretRotated}",
                webhookId.Value, userId.Value, request.Url, eventTypes.Count, secretRotated);

            return Ok(MapToDto(updated));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation error updating webhook");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating webhook {WebhookId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the webhook" });
        }
    }

    /// <summary>
    /// Tests a webhook by sending a test payload.
    /// POST /api/webhooks/{id}/test
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<ActionResult<WebhookTestResponseDto>> TestWebhook(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(id, out var webhookGuid))
                return BadRequest(new { message = "Invalid webhook ID format" });

            var webhook = await _webhookRepository.GetByIdAsync(WebhookId.From(webhookGuid), cancellationToken);
            if (webhook == null)
                return NotFound(new { message = "Webhook not found" });

            var userId = _userContext.CurrentUserId;
            if (webhook.CreatedBy.Value != userId.Value)
                return Forbid();

            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var testPayload = new { test = true, message = "Webhook test from Arc", timestamp = DateTime.UtcNow };
            var json = System.Text.Json.JsonSerializer.Serialize(testPayload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(webhook.Url, content, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("Webhook test completed: {WebhookId}, Status: {StatusCode}, Time: {ResponseTime}ms",
                webhook.Id.Value, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

            return Ok(new WebhookTestResponseDto
            {
                Success = response.IsSuccessStatusCode,
                ResponseCode = (int)response.StatusCode,
                ResponseTime = (int)stopwatch.ElapsedMilliseconds
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Webhook test failed for {WebhookId}", id);
            return Ok(new WebhookTestResponseDto
            {
                Success = false,
                ResponseCode = 0,
                ResponseTime = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing webhook {WebhookId}", id);
            return StatusCode(500, new { message = "An error occurred while testing the webhook" });
        }
    }

    private static WebhookResponseDto MapToDto(Webhook webhook)
    {
        return new WebhookResponseDto
        {
            Id = webhook.Id.Value.ToString(),
            Url = webhook.Url,
            Events = webhook.Events.Select(e => e.ToEventString()).ToList(),
            IsActive = webhook.IsActive,
            CreatedAt = webhook.CreatedAt
        };
    }

    private static WebhookListItemDto MapToListItemDto(Webhook webhook)
    {
        return new WebhookListItemDto
        {
            Id = webhook.Id.Value.ToString(),
            Url = webhook.Url,
            Events = webhook.Events.Select(e => e.ToEventString()).ToList(),
            IsActive = webhook.IsActive,
            CreatedAt = webhook.CreatedAt
        };
    }
}