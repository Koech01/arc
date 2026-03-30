namespace Arc.Application.Admin;


/// <summary>
/// Type of action performed by an admin.
/// </summary>
public enum AdminAuditAction
{
    ViewedUserList,
    ViewedUserDetail,
    ViewedStats,
    ActivatedUser,
    DeactivatedUser,
    ChangedUserRole,
    ResetUserPassword,
    DeletedUser,
    ViewedAllExecutions,
    ViewedLLMConfigs,
    ViewedWebhooks,
    DisabledWebhook,
    ClearedCache,
    ClearedUserCache,
    ViewedCacheStats,
    EnabledMaintenanceMode,
    DisabledMaintenanceMode,
    ViewedSystemConfig,
    ViewedLoginHistory,
    ViewedAdminAuditLog
}

/// <summary>
/// Immutable record representing a single admin action event.
/// </summary>
public sealed record AdminAuditEvent(
    Guid AdminUserId,
    AdminAuditAction Action,
    DateTime TimestampUtc,
    string? TargetUserId = null,
    string? Detail = null,
    string? IpAddress = null,
    string? UserAgent = null
);

/// <summary>
/// Append-only audit logger for privileged admin actions.
/// Every mutating or sensitive read action performed via admin endpoints must be logged here.
/// </summary>
public interface IAdminAuditLogger
{
    /// <summary>Records an admin action event asynchronously. Never throws - errors are swallowed and logged internally.</summary>
    Task LogAsync(AdminAuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all admin audit entries optionally filtered by admin user or action type.</summary>
    Task<IReadOnlyList<AdminAuditEntry>> GetLogsAsync(
        Guid? adminUserId = null,
        AdminAuditAction? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default);
}

/// <summary>Persisted admin audit log entry returned from queries.</summary>
public sealed record AdminAuditEntry(
    long Id,
    Guid AdminUserId,
    string AdminAuditAction,
    DateTime TimestampUtc,
    string? TargetUserId,
    string? Detail,
    string? IpAddress,
    string? UserAgent
);