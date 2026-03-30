using Arc.Api.Filters;
using Arc.Domain.Models;
using Arc.Api.DTOs.Admin;
using Arc.Application.Admin;
using Arc.Application.Identity;
using Arc.Application.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;
using Microsoft.AspNetCore.Authorization;


namespace Arc.Api.Controllers;
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[ServiceFilter(typeof(AdminActionLoggingFilter))]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminStatsService _statsService;
    private readonly IAdminUserService _userService;
    private readonly IAdminAuditLogger _auditLogger;
    private readonly ILoginHistoryRepository _loginHistory;
    private readonly IWebhookRepository _webhookRepo;
    private readonly ITaskExecutionCache _cache;
    private readonly IMaintenanceModeService _maintenanceMode;
    private readonly ISystemConfigurationService _sysConfig;
    private readonly IUserContext _userContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAdminStatsService statsService,
        IAdminUserService userService,
        IAdminAuditLogger auditLogger,
        ILoginHistoryRepository loginHistory,
        IWebhookRepository webhookRepo,
        ITaskExecutionCache cache,
        IMaintenanceModeService maintenanceMode,
        ISystemConfigurationService sysConfig,
        IUserContext userContext,
        ILogger<AdminController> logger)
    {
        _statsService = statsService;
        _userService = userService;
        _auditLogger = auditLogger;
        _loginHistory = loginHistory;
        _webhookRepo = webhookRepo;
        _cache = cache;
        _maintenanceMode = maintenanceMode;
        _sysConfig = sysConfig;
        _userContext = userContext;
        _logger = logger;
    }

    // Dashboard 

    /// <summary>Returns aggregated dashboard statistics.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsResponseDto>> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _statsService.GetStatsAsync(cancellationToken);
        return Ok(new AdminStatsResponseDto
        {
            TotalUsers = stats.TotalUsers,
            ActiveUsers = stats.ActiveUsers,
            NewUsersThisWeek = stats.NewUsersThisWeek,
            ActiveLLMs = stats.ActiveLLMs,
            NewLLMsThisWeek = stats.NewLLMsThisWeek,
            TotalExecutions = stats.TotalExecutions,
            ExecutionsToday = stats.ExecutionsToday
        });
    }

    // Users (legacy flat list) 

    /// <summary>Legacy non-paginated user list. Use GET /users/search for filtering.</summary>
    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _statsService.ListUsersAsync(cancellationToken);
        return Ok(users.Select(u => new AdminUserDto
        {
            Id = u.Id,
            Email = u.Email,
            Role = u.Role,
            Status = u.Status,
            CreatedAt = u.CreatedAt
        }).ToList());
    }

    // Users (paginated / filtered) 

    /// <summary>Paginated, filterable user list.</summary>
    [HttpGet("users/search")]
    public async Task<ActionResult<AdminUserPageDto>> SearchUsers(
        [FromQuery] string? email = null,
        [FromQuery] string? username = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        UserRole? parsedRole = null;
        if (role is not null && !Enum.TryParse<UserRole>(role, ignoreCase: true, out var r))
            return BadRequest(new { message = $"Invalid role: {role}" });
        else if (role is not null)
            parsedRole = (UserRole)Enum.Parse(typeof(UserRole), role, ignoreCase: true);

        var filter = new AdminUserFilter(email, username, parsedRole, isActive, includeDeleted);
        var result = await _userService.QueryUsersAsync(filter, Math.Clamp(limit, 1, 200), Math.Max(0, offset), cancellationToken);

        return Ok(new AdminUserPageDto(
            result.Users.Select(MapUserDetail).ToList(),
            result.TotalCount,
            result.Limit,
            result.Offset
        ));
    }

    /// <summary>Gets full detail for a single user by ID.</summary>
    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<AdminUserDetailDto>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (user is null) return NotFound();
        return Ok(MapUserDetail(user));
    }

    /// <summary>Activates or deactivates a user account.</summary>
    [HttpPatch("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var adminId = _userContext.CurrentUserId.Value;
        if (request.IsActive)
            await _userService.ActivateUserAsync(id, adminId, cancellationToken);
        else
            await _userService.DeactivateUserAsync(id, adminId, cancellationToken);

        return NoContent();
    }

    /// <summary>Changes a user's role.</summary>
    [HttpPatch("users/{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(
        Guid id,
        [FromBody] UpdateUserRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var newRole))
            return BadRequest(new { message = $"Invalid role: {request.Role}" });

        await _userService.ChangeUserRoleAsync(id, newRole, _userContext.CurrentUserId.Value, cancellationToken);
        return NoContent();
    }

    /// <summary>Admin-initiated password reset for a user.</summary>
    [HttpPost("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(
        Guid id,
        [FromBody] AdminResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest(new { message = "Password must be at least 8 characters." });

        await _userService.ResetUserPasswordAsync(id, request.NewPassword, _userContext.CurrentUserId.Value, cancellationToken);
        return NoContent();
    }

    /// <summary>Soft-deletes a user account. The record is preserved for audit purposes.</summary>
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeleteUserAsync(id, _userContext.CurrentUserId.Value, cancellationToken);
        return NoContent();
    }

    /// <summary>Returns recent login history for a user.</summary>
    [HttpGet("users/{id:guid}/login-history")]
    public async Task<ActionResult<IReadOnlyList<LoginHistoryEntryDto>>> GetLoginHistory(
        Guid id,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var entries = await _loginHistory.GetByUserIdAsync(
            new UserId(id), Math.Clamp(limit, 1, 200), cancellationToken);

        return Ok(entries.Select(e => new LoginHistoryEntryDto(
            e.Id,
            e.TimestampUtc,
            e.Success,
            e.FailureReason,
            e.IpAddress,
            e.UserAgent
        )).ToList());
    }

    // Executions 

    /// <summary>System-wide paginated execution list.</summary>
    [HttpGet("executions")]
    public async Task<ActionResult<AdminExecutionPageDto>> GetExecutions(
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var overview = await _statsService.GetSystemExecutionsAsync(
            status, from, to, Math.Clamp(limit, 1, 200), Math.Max(0, offset), cancellationToken);

        return Ok(new AdminExecutionPageDto(
            overview.Executions.Select(e => new AdminExecutionRowDto(
                e.ExecutionId, e.UserId, e.UserEmail, e.Status,
                e.CreatedAtUtc, e.TaskCount, e.ExecutionTimeMs, e.WorkflowName
            )).ToList(),
            overview.TotalCount,
            overview.Limit,
            overview.Offset
        ));
    }

    // LLM Configurations

    /// <summary>System-wide paginated LLM configuration list.</summary>
    [HttpGet("llm-configs")]
    public async Task<ActionResult<IReadOnlyList<AdminLLMConfigDto>>> GetLLMConfigs(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var configs = await _statsService.GetAllLLMConfigsAsync(
            Math.Clamp(limit, 1, 200), Math.Max(0, offset), cancellationToken);

        return Ok(configs.Select(c => new AdminLLMConfigDto(
            c.Id, c.Name, c.Model, c.BaseUrl, c.IsActive, c.CreatedAt, c.OwnerEmail
        )).ToList());
    }

    // Webhooks 

    /// <summary>System-wide paginated webhook list.</summary>
    [HttpGet("webhooks")]
    public async Task<ActionResult<AdminWebhookPageDto>> GetWebhooks(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var (webhooks, total) = await _webhookRepo.GetAllAsync(
            Math.Clamp(limit, 1, 200), Math.Max(0, offset), cancellationToken);

        return Ok(new AdminWebhookPageDto(
            webhooks.Select(w => new AdminWebhookDto(
                w.Id.Value,
                w.Url.ToString(),
                w.Events.Select(e => e.ToString()).ToList(),
                w.IsActive,
                w.CreatedBy.Value,
                w.CreatedAt
            )).ToList(),
            total,
            Math.Clamp(limit, 1, 200),
            Math.Max(0, offset)
        ));
    }

    /// <summary>Deactivates all webhooks belonging to a specific user.</summary>
    [HttpPatch("webhooks/user/{userId:guid}/deactivate")]
    public async Task<ActionResult<object>> DeactivateUserWebhooks(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var count = await _webhookRepo.DeactivateByUserIdAsync(new UserId(userId), cancellationToken);
        return Ok(new { deactivatedCount = count });
    }

    // Cache 

    /// <summary>Returns task execution cache occupancy statistics.</summary>
    [HttpGet("cache/stats")]
    public async Task<ActionResult<AdminCacheStatsDto>> GetCacheStats(CancellationToken cancellationToken)
    {
        var stats = await _cache.GetStatsAsync(cancellationToken);
        return Ok(new AdminCacheStatsDto(
            stats.TotalEntries,
            stats.ExpiredEntries,
            stats.ActiveEntries,
            stats.OldestEntryUtc,
            stats.NewestEntryUtc
        ));
    }

    /// <summary>Invalidates all task execution cache entries.</summary>
    [HttpDelete("cache")]
    public async Task<IActionResult> ClearCache(CancellationToken cancellationToken)
    {
        await _cache.InvalidateAsync();
        _logger.LogInformation("Task execution cache cleared by admin {AdminId}", _userContext.CurrentUserId.Value);
        return NoContent();
    }

    // Maintenance Mode 

    /// <summary>Returns the current maintenance mode status.</summary>
    [HttpGet("maintenance")]
    public ActionResult<MaintenanceModeStatusDto> GetMaintenanceStatus()
    {
        var status = _maintenanceMode.GetStatus();
        return Ok(new MaintenanceModeStatusDto(
            status.IsEnabled,
            status.EnabledBy,
            status.EnabledAtUtc,
            status.Reason
        ));
    }

    /// <summary>Enables maintenance mode. Non-admin requests will receive HTTP 503.</summary>
    [HttpPost("maintenance/enable")]
    public IActionResult EnableMaintenance([FromBody] EnableMaintenanceModeRequestDto request)
    {
        _maintenanceMode.Enable(_userContext.CurrentUserId.Value, request.Reason);
        _logger.LogWarning("Maintenance mode ENABLED by admin {AdminId}. Reason: {Reason}",
            _userContext.CurrentUserId.Value, request.Reason ?? "(none)");
        return NoContent();
    }

    /// <summary>Disables maintenance mode.</summary>
    [HttpDelete("maintenance/disable")]
    public IActionResult DisableMaintenance()
    {
        _maintenanceMode.Disable(_userContext.CurrentUserId.Value);
        _logger.LogInformation("Maintenance mode DISABLED by admin {AdminId}", _userContext.CurrentUserId.Value);
        return NoContent();
    }

    // System Configuration

    /// <summary>Returns a read-only snapshot of the current system configuration.</summary>
    [HttpGet("system")]
    public ActionResult<SystemConfigDto> GetSystemConfig()
    {
        var snapshot = _sysConfig.GetSnapshot();
        return Ok(new SystemConfigDto(
            snapshot.DatabaseProvider,
            snapshot.LLMDefaultProvider,
            snapshot.LLMDefaultModel,
            snapshot.JwtExpirationMinutes,
            snapshot.RateLimitPermitLimit,
            snapshot.RateLimitWindowSeconds,
            snapshot.MaintenanceModeEnabled,
            snapshot.Environment,
            snapshot.ApiVersion
        ));
    }

    // Admin Audit Log

    /// <summary>Returns paginated admin audit log entries with optional filters.</summary>
    [HttpGet("audit-log")]
    public async Task<ActionResult<IReadOnlyList<AdminAuditEntryDto>>> GetAuditLog(
        [FromQuery] Guid? adminUserId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        AdminAuditAction? parsedAction = null;
        if (action is not null)
        {
            if (!Enum.TryParse<AdminAuditAction>(action, ignoreCase: true, out var a))
                return BadRequest(new { message = $"Invalid action: {action}" });
            parsedAction = a;
        }

        var entries = await _auditLogger.GetLogsAsync(
            adminUserId, parsedAction, from, to,
            Math.Clamp(limit, 1, 500), Math.Max(0, offset),
            cancellationToken);

        return Ok(entries.Select(e => new AdminAuditEntryDto(
            e.Id,
            e.AdminUserId,
            e.AdminAuditAction,
            e.TimestampUtc,
            e.TargetUserId,
            e.Detail,
            e.IpAddress,
            e.UserAgent
        )).ToList());
    }

    // Helpers 

    private static AdminUserDetailDto MapUserDetail(AdminUserDetail u) =>
        new(u.Id, u.Username, u.Email, u.Role, u.Status, u.CreatedAt,
            u.IsLockedOut, u.LockedUntilUtc, u.FailedLoginAttempts,
            u.IsDeleted, u.DeletedAt, u.Firstname);
}