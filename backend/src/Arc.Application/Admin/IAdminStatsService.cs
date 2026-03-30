namespace Arc.Application.Admin;


/// <summary>
/// Service for retrieving admin dashboard statistics and cross-system oversight data.
/// </summary>
public interface IAdminStatsService
{
    /// <summary>Gets dashboard statistics. Results are cached for 30 seconds.</summary>
    Task<AdminStats> GetStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists all non-deleted users in the system (legacy flat list without pagination).</summary>
    Task<IReadOnlyList<UserInfo>> ListUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>Admin-only: paginated view of all executions across all users.</summary>
    Task<AdminExecutionOverview> GetSystemExecutionsAsync(
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    /// <summary>Admin-only: paginated view of all LLM configurations across all users.</summary>
    Task<IReadOnlyList<AdminLLMConfigRow>> GetAllLLMConfigsAsync(
        int limit,
        int offset,
        CancellationToken cancellationToken = default);
}

/// <summary>Admin dashboard statistics.</summary>
public sealed record AdminStats(
    int TotalUsers,
    int ActiveUsers,
    int NewUsersThisWeek,
    int ActiveLLMs,
    int NewLLMsThisWeek,
    int TotalExecutions,
    int ExecutionsToday
);

/// <summary>User information for legacy admin dashboard list.</summary>
public sealed record UserInfo(
    Guid Id,
    string Username,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt
);

/// <summary>Single execution row in the system-wide admin execution table.</summary>
public sealed record AdminExecutionRow(
    string ExecutionId,
    Guid UserId,
    string UserEmail,
    string Status,
    DateTime CreatedAtUtc,
    int TaskCount,
    long ExecutionTimeMs,
    string? WorkflowName
);

/// <summary>Paginated system-wide execution overview.</summary>
public sealed record AdminExecutionOverview(
    IReadOnlyList<AdminExecutionRow> Executions,
    int TotalCount,
    int Limit,
    int Offset
);

/// <summary>Single LLM configuration row in the admin config table.</summary>
public sealed record AdminLLMConfigRow(
    string Id,
    string Name,
    string Model,
    string BaseUrl,
    bool IsActive,
    DateTime CreatedAt,
    string OwnerEmail
);