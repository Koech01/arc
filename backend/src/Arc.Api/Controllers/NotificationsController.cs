using Arc.Domain.Models;
using Arc.Application.Identity;
using Microsoft.AspNetCore.Mvc;
using Arc.Api.DTOs.Notifications;
using Arc.Application.Notifications;
using Microsoft.AspNetCore.Authorization;


namespace Arc.Api.Controllers;
/// <summary>
/// Notifications controller for managing user notifications.
/// Provides endpoints for listing notifications and marking them as read.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserContext _userContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationRepository notificationRepository,
        IUserContext userContext,
        ILogger<NotificationsController> logger)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all notifications for the authenticated user.
    /// GET /api/notifications
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationResponseDto>>> ListNotifications(
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _userContext.CurrentUserId;
            var notifications = await _notificationRepository.ListByUserAsync(userId, cancellationToken);

            var dtos = notifications.Select(MapToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing notifications for user {UserId}", _userContext.CurrentUserId.Value);
            return StatusCode(500, new { message = "An error occurred while listing notifications" });
        }
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// PUT /api/notifications/{id}/read
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(id, out var notificationGuid))
                return BadRequest(new { message = "Invalid notification ID format" });

            var notificationId = NotificationId.From(notificationGuid);
            var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            var userId = _userContext.CurrentUserId;
            if (notification.UserId.Value != userId.Value)
                return Forbid();

            if (!notification.IsRead)
            {
                var updatedNotification = notification.MarkAsRead();
                await _notificationRepository.UpdateAsync(updatedNotification, cancellationToken);
                _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}", notificationId.Value, userId.Value);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return StatusCode(500, new { message = "An error occurred while updating the notification" });
        }
    }

    /// <summary>
    /// Marks all notifications as read for the authenticated user.
    /// PUT /api/notifications/read-all
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _userContext.CurrentUserId;
            var updatedCount = await _notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);

            _logger.LogInformation("{Count} notifications marked as read for user {UserId}", updatedCount, userId.Value);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", _userContext.CurrentUserId.Value);
            return StatusCode(500, new { message = "An error occurred while updating notifications" });
        }
    }

    /// <summary>
    /// Deletes a specific notification.
    /// DELETE /api/notifications/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(id, out var notificationGuid))
                return BadRequest(new { message = "Invalid notification ID format" });

            var notificationId = NotificationId.From(notificationGuid);
            var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            var userId = _userContext.CurrentUserId;
            if (notification.UserId.Value != userId.Value)
                return Forbid();

            await _notificationRepository.DeleteAsync(notificationId, cancellationToken);
            _logger.LogInformation("Notification {NotificationId} deleted by user {UserId}", notificationId.Value, userId.Value);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the notification" });
        }
    }

    private static NotificationResponseDto MapToDto(Notification notification)
    {
        return new NotificationResponseDto
        {
            Id = notification.Id.Value.ToString(),
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type.ToTypeString(),
            Read = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}