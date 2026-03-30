namespace Arc.Api.Filters;
using Arc.Application.Admin;
using Arc.Application.Identity;
using Microsoft.AspNetCore.Mvc.Filters;


/// <summary>
/// ASP.NET Core action filter applied to the AdminController.
/// Records every admin request to the <see cref="IAdminAuditLogger"/> and logs
/// the request context (IP, user agent, path) via structured logging.
/// </summary>
public sealed class AdminActionLoggingFilter : IAsyncActionFilter
{
    private readonly IAdminAuditLogger _auditLogger;
    private readonly IUserContext _userContext;
    private readonly ILogger<AdminActionLoggingFilter> _logger;

    public AdminActionLoggingFilter(
        IAdminAuditLogger auditLogger,
        IUserContext userContext,
        ILogger<AdminActionLoggingFilter> logger)
    {
        _auditLogger = auditLogger;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var adminId = _userContext.CurrentUserId.Value;
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        var method = context.HttpContext.Request.Method;
        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = context.HttpContext.Request.Headers.UserAgent.FirstOrDefault();

        _logger.LogInformation(
            "Admin request: AdminId={AdminId} Method={Method} Path={Path} IP={IP}",
            adminId, method, path, ip ?? "unknown");

        var action = InferAction(method, path);
        if (action.HasValue)
        {
            await _auditLogger.LogAsync(new AdminAuditEvent(
                adminId,
                action.Value,
                DateTime.UtcNow,
                IpAddress: ip,
                UserAgent: ua
            ), context.HttpContext.RequestAborted);
        }

        await next();
    }

    private static AdminAuditAction? InferAction(string method, string path)
    {
        var lower = path.ToLowerInvariant();

        if (method == "GET" && lower.Contains("/admin/stats")) return AdminAuditAction.ViewedStats;
        if (method == "GET" && lower.Contains("/admin/users") && !lower.Contains("/login-history"))
            return AdminAuditAction.ViewedUserList;
        if (method == "GET" && lower.Contains("/login-history")) return AdminAuditAction.ViewedLoginHistory;
        if (method == "PATCH" && lower.Contains("/status")) return AdminAuditAction.ActivatedUser;
        if (method == "PATCH" && lower.Contains("/role")) return AdminAuditAction.ChangedUserRole;
        if (method == "POST" && lower.Contains("/reset-password")) return AdminAuditAction.ResetUserPassword;
        if (method == "DELETE" && lower.Contains("/admin/users")) return AdminAuditAction.DeletedUser;
        if (method == "GET" && lower.Contains("/admin/executions")) return AdminAuditAction.ViewedAllExecutions;
        if (method == "GET" && lower.Contains("/admin/llm-configs")) return AdminAuditAction.ViewedLLMConfigs;
        if (method == "GET" && lower.Contains("/admin/webhooks")) return AdminAuditAction.ViewedWebhooks;
        if (method == "PATCH" && lower.Contains("/admin/webhooks")) return AdminAuditAction.DisabledWebhook;
        if (method == "DELETE" && lower.Contains("/admin/cache")) return AdminAuditAction.ClearedCache;
        if (method == "GET" && lower.Contains("/admin/cache")) return AdminAuditAction.ViewedCacheStats;
        if (method == "POST" && lower.Contains("/maintenance")) return AdminAuditAction.EnabledMaintenanceMode;
        if (method == "DELETE" && lower.Contains("/maintenance")) return AdminAuditAction.DisabledMaintenanceMode;
        if (method == "GET" && lower.Contains("/admin/system")) return AdminAuditAction.ViewedSystemConfig;
        if (method == "GET" && lower.Contains("/admin/audit-log")) return AdminAuditAction.ViewedAdminAuditLog;

        return null;
    }
}