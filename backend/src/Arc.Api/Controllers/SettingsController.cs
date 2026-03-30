using FluentValidation;
using Arc.Domain.Models;
using Arc.Api.DTOs.Settings;
namespace Arc.Api.Controllers;
using Arc.Application.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


/// <summary>
/// Settings controller for managing user preferences.
/// Provides endpoints for retrieving and updating user preferences.
/// </summary>
[ApiController]
[Route("api/settings")]
[Authorize]
public sealed class SettingsController : ControllerBase
{
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IUserContext _userContext;
    private readonly IValidator<UpdateUserPreferencesRequestDto> _validator;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IUserPreferencesRepository preferencesRepository,
        IUserContext userContext,
        IValidator<UpdateUserPreferencesRequestDto> validator,
        ILogger<SettingsController> logger)
    {
        _preferencesRepository = preferencesRepository ?? throw new ArgumentNullException(nameof(preferencesRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets user preferences for the authenticated user.
    /// Returns default preferences if none exist.
    /// GET /api/settings/preferences
    /// </summary>
    [HttpGet("preferences")]
    public async Task<ActionResult<UserPreferencesResponseDto>> GetPreferences(
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _userContext.CurrentUserId;
            var preferences = await _preferencesRepository.GetByUserIdAsync(userId, cancellationToken);

            // Return default preferences if none exist
            if (preferences == null)
            {
                preferences = UserPreferences.CreateDefault(userId);
            }

            return Ok(MapToDto(preferences));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving preferences for user {UserId}", _userContext.CurrentUserId.Value);
            return StatusCode(500, new { message = "An error occurred while retrieving preferences" });
        }
    }

    /// <summary>
    /// Updates user preferences for the authenticated user.
    /// Creates preferences if they don't exist.
    /// PUT /api/settings/preferences
    /// </summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateUserPreferencesRequestDto request,
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

            var preferences = new UserPreferences(
                userId,
                request.Theme,
                request.Notifications.Email,
                request.Notifications.Push,
                request.Notifications.ExecutionComplete,
                request.Notifications.ExecutionFailed,
                request.Language,
                request.Timezone);

            await _preferencesRepository.UpsertAsync(preferences, cancellationToken);

            _logger.LogInformation("Preferences updated for user {UserId}", userId.Value);

            return Ok();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid preferences data for user {UserId}", _userContext.CurrentUserId.Value);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for user {UserId}", _userContext.CurrentUserId.Value);
            return StatusCode(500, new { message = "An error occurred while updating preferences" });
        }
    }

    private static UserPreferencesResponseDto MapToDto(UserPreferences preferences)
    {
        return new UserPreferencesResponseDto
        {
            Theme = preferences.Theme,
            Notifications = new NotificationPreferencesDto
            {
                Email = preferences.NotificationEmail,
                Push = preferences.NotificationPush,
                ExecutionComplete = preferences.NotificationExecutionComplete,
                ExecutionFailed = preferences.NotificationExecutionFailed
            },
            Language = preferences.Language,
            Timezone = preferences.Timezone
        };
    }
}
