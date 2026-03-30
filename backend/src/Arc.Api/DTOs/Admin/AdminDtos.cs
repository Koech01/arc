namespace Arc.Api.DTOs.Admin;

// ── Dashboard Stats ────────────────────────────────────────────────────────────

public sealed class AdminStatsResponseDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int ActiveLLMs { get; set; }
    public int NewLLMsThisWeek { get; set; }
    public int TotalExecutions { get; set; }
    public int ExecutionsToday { get; set; }
}

// ── User Management ────────────────────────────────────────────────────────────

/// <summary>Legacy flat user row returned by GET /api/admin/users (non-paginated).</summary>
public sealed class AdminUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Rich user detail returned by GET /api/admin/users/{id} and paginated query.</summary>
public sealed record AdminUserDetailDto(
    Guid Id,
    string Username,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    bool IsLockedOut,
    DateTime? LockedUntilUtc,
    int FailedLoginAttempts,
    bool IsDeleted,
    DateTime? DeletedAt,
    string? Firstname
);

/// <summary>Paginated user list response.</summary>
public sealed record AdminUserPageDto(
    IReadOnlyList<AdminUserDetailDto> Users,
    int TotalCount,
    int Limit,
    int Offset
);

/// <summary>Request body for PATCH /api/admin/users/{id}/status</summary>
public sealed record UpdateUserStatusRequestDto(bool IsActive);

/// <summary>Request body for PATCH /api/admin/users/{id}/role</summary>
public sealed record UpdateUserRoleRequestDto(string Role);

/// <summary>Request body for POST /api/admin/users/{id}/reset-password</summary>
public sealed record AdminResetPasswordRequestDto(string NewPassword);

// ── Executions ─────────────────────────────────────────────────────────────────

public sealed record AdminExecutionRowDto(
    string ExecutionId,
    Guid UserId,
    string UserEmail,
    string Status,
    DateTime CreatedAtUtc,
    int TaskCount,
    long ExecutionTimeMs,
    string? WorkflowName
);

public sealed record AdminExecutionPageDto(
    IReadOnlyList<AdminExecutionRowDto> Executions,
    int TotalCount,
    int Limit,
    int Offset
);

// ── LLM Configurations ─────────────────────────────────────────────────────────

public sealed record AdminLLMConfigDto(
    string Id,
    string Name,
    string Model,
    string BaseUrl,
    bool IsActive,
    DateTime CreatedAt,
    string OwnerEmail
);

// ── Webhooks ───────────────────────────────────────────────────────────────────

public sealed record AdminWebhookDto(
    Guid Id,
    string Url,
    IReadOnlyList<string> Events,
    bool IsActive,
    Guid CreatedBy,
    DateTime CreatedAt
);

public sealed record AdminWebhookPageDto(
    IReadOnlyList<AdminWebhookDto> Webhooks,
    int TotalCount,
    int Limit,
    int Offset
);

// ── Cache Stats ────────────────────────────────────────────────────────────────

public sealed record AdminCacheStatsDto(
    int TotalEntries,
    int ExpiredEntries,
    int ActiveEntries,
    DateTime? OldestEntryUtc,
    DateTime? NewestEntryUtc
);

// ── Login History ──────────────────────────────────────────────────────────────

public sealed record LoginHistoryEntryDto(
    long Id,
    DateTime TimestampUtc,
    bool Success,
    string? FailureReason,
    string? IpAddress,
    string? UserAgent
);

// ── Audit Log ──────────────────────────────────────────────────────────────────

public sealed record AdminAuditEntryDto(
    long Id,
    Guid AdminUserId,
    string Action,
    DateTime TimestampUtc,
    string? TargetUserId,
    string? Detail,
    string? IpAddress,
    string? UserAgent
);

// ── Maintenance Mode ───────────────────────────────────────────────────────────

public sealed record MaintenanceModeStatusDto(
    bool IsEnabled,
    Guid? EnabledBy,
    DateTime? EnabledAtUtc,
    string? Reason
);

/// <summary>Request body for POST /api/admin/maintenance/enable</summary>
public sealed record EnableMaintenanceModeRequestDto(string? Reason = null);

// ── System Configuration ───────────────────────────────────────────────────────

public sealed record SystemConfigDto(
    string DatabaseProvider,
    string LLMDefaultProvider,
    string LLMDefaultModel,
    int JwtExpirationMinutes,
    int RateLimitPermitLimit,
    int RateLimitWindowSeconds,
    bool MaintenanceModeEnabled,
    string Environment,
    string ApiVersion
);