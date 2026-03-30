using Arc.Application.Results;
namespace Arc.Application.Execution;


/// <summary>
/// Query filter for execution results.
/// All filters are optional; null values mean "no filter for this criterion".
/// </summary>
public sealed record ExecutionQueryFilter(
    string? Status,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    int? MinTaskCount,
    int? MaxTaskCount,
    int? MinAverageExecutionTimeMs,
    int? MaxAverageExecutionTimeMs,
    bool? IncludeArchived
);

/// <summary>
/// Paginated query parameters.
/// </summary>
public sealed record PaginationParams(
    int Limit = 10,
    int Offset = 0
)
{
    public static PaginationParams Validate(int? limit, int? offset)
    {
        var validLimit = limit ?? 10;
        var validOffset = offset ?? 0;

        if (validLimit < 1 || validLimit > 1000)
            validLimit = Math.Min(Math.Max(1, validLimit), 1000);

        if (validOffset < 0)
            validOffset = 0;

        return new PaginationParams(validLimit, validOffset);
    }
}

/// <summary>
/// Single execution metadata for list results.
/// Includes workflow display context (WorkflowName, WorkflowDescription) required by the UI.
/// </summary>
public sealed record ExecutionMetadata(
    string ExecutionId,
    DateTime CreatedAtUtc,
    int TaskCount,
    long AverageExecutionTimeMs,
    string Status,
    string WorkflowName,
    string WorkflowDescription,
    bool IsArchived
);

/// <summary>
/// Aggregated analytics for execution results.
/// </summary>
public sealed record ExecutionAnalytics(
    long TotalCount,
    long SuccessCount,
    long FailureCount,
    double SuccessRate,
    long AverageTaskCount,
    long AverageExecutionTimeMs
);

/// <summary>
/// Paginated result set for execution queries.
/// </summary>
public sealed record ExecutionQueryResult(
    IReadOnlyList<ExecutionMetadata> Executions,
    ExecutionAnalytics Analytics,
    int Limit,
    int Offset,
    long TotalAvailable
);

public interface IExecutionResultStore
{
    // Convenience overloads - delegate to the canonical method 
    // Call sites that do not supply a workflow context continue to compile without
    // modification; the default implementations inject null context automatically.

    Task StoreAsync(string executionId, ExecutionResult result)
        => StoreAsync(executionId, result, DateTime.UtcNow, null);

    Task StoreAsync(string executionId, ExecutionResult result, DateTime createdAtUtc)
        => StoreAsync(executionId, result, createdAtUtc, null);

    Task StoreAsync(string executionId, ExecutionResult result, ExecutionWorkflowContext? workflowContext)
        => StoreAsync(executionId, result, DateTime.UtcNow, workflowContext);

    /// <summary>
    /// Canonical store method. All other overloads delegate here.
    /// Implementations must provide this method only.
    /// </summary>
    Task StoreAsync(
        string executionId,
        ExecutionResult result,
        DateTime createdAtUtc,
        ExecutionWorkflowContext? workflowContext);

    Task<ExecutionResult?> GetAsync(string executionId);

    /// <summary>
    /// Returns the workflow context (name, description) stored alongside an execution.
    /// Returns null when the execution does not exist.
    /// </summary>
    Task<ExecutionWorkflowContext?> GetWorkflowContextAsync(string executionId);

    Task<ExecutionQueryResult> QueryAsync(ExecutionQueryFilter? filter, PaginationParams pagination, Guid userId);

    /// <summary>
    /// Archives an execution (soft delete). Archived executions are hidden from normal queries.
    /// </summary>
    Task ArchiveAsync(string executionId, Guid archivedBy, string? reason = null, int? retentionDays = null);

    /// <summary>
    /// Unarchives an execution, making it visible in normal queries again.
    /// </summary>
    Task UnarchiveAsync(string executionId, Guid unarchivedBy);

    /// <summary>
    /// Permanently deletes an execution (hard delete). Only for admin use.
    /// </summary>
    Task PurgeAsync(string executionId, Guid purgedBy, string? reason = null);

    /// <summary>
    /// Gets archive audit trail for an execution.
    /// </summary>
    Task<IReadOnlyList<ArchiveAuditEntry>> GetArchiveAuditAsync(string executionId);
}


/// <summary>
/// Archive audit trail entry.
/// </summary>
public sealed record ArchiveAuditEntry(
    long Id,
    string ExecutionId,
    string Action,
    Guid PerformedBy,
    DateTime PerformedAtUtc,
    string? Reason,
    string? IpAddress,
    string? UserAgent
);