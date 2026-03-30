using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;
using System.Collections.Concurrent;
namespace Arc.Infrastructure.Execution;


public sealed class InMemoryExecutionResultStore : IExecutionResultStore
{
    private readonly ConcurrentDictionary<string, ExecutionResultEntry> _store = new();

    private sealed record ExecutionResultEntry(
        string ExecutionId,
        ExecutionResult Result,
        DateTime CreatedAtUtc,
        ExecutionWorkflowContext? WorkflowContext,
        bool IsArchived = false
    )
    {
        public string UserId => Result.UserId.ToString();
    }

    // ── Convenience overloads - mirror the interface default implementations so callers
    // using the concrete type (e.g. unit tests) also get the short-hand signatures. ──

    public Task StoreAsync(string executionId, ExecutionResult result)
        => StoreAsync(executionId, result, DateTime.UtcNow, null);

    public Task StoreAsync(string executionId, ExecutionResult result, DateTime createdAtUtc)
        => StoreAsync(executionId, result, createdAtUtc, null);

    public Task StoreAsync(string executionId, ExecutionResult result, ExecutionWorkflowContext? workflowContext)
        => StoreAsync(executionId, result, DateTime.UtcNow, workflowContext);

    // ── Canonical store ──────────────────────────────────────────────────────

    public Task StoreAsync(
        string executionId,
        ExecutionResult result,
        DateTime createdAtUtc,
        ExecutionWorkflowContext? workflowContext)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            throw new ArgumentException("Execution ID must not be null or empty.", nameof(executionId));

        if (result is null)
            throw new ArgumentNullException(nameof(result));

        _store[executionId] = new ExecutionResultEntry(executionId, result, createdAtUtc, workflowContext);
        return Task.CompletedTask;
    }

    public Task<ExecutionResult?> GetAsync(string executionId)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            throw new ArgumentException("Execution ID must not be null or empty.", nameof(executionId));

        _store.TryGetValue(executionId, out var entry);
        return Task.FromResult(entry?.Result);
    }

    public Task<ExecutionWorkflowContext?> GetWorkflowContextAsync(string executionId)
    {
        _store.TryGetValue(executionId, out var entry);
        return Task.FromResult(entry?.WorkflowContext);
    }

    public Task<ExecutionQueryResult> QueryAsync(ExecutionQueryFilter? filter, PaginationParams pagination)
        => QueryAsync(filter, pagination, UserId.Anonymous.Value);

    public Task<ExecutionQueryResult> QueryAsync(ExecutionQueryFilter? filter, PaginationParams pagination, Guid userId)
    {
        var allEntries = _store.Values.Where(e => e.Result.UserId.Value == userId).ToList();

        var filtered = allEntries
            .Where(e => MatchesFilter(e, filter))
            .OrderBy(e => e.ExecutionId)
            .ToList();

        var totalAvailable = filtered.Count;

        var paginated = filtered
            .Skip(pagination.Offset)
            .Take(pagination.Limit)
            .ToList();

        var executions = paginated
            .Select(e => new ExecutionMetadata(
                ExecutionId: e.ExecutionId,
                CreatedAtUtc: e.CreatedAtUtc,
                TaskCount: e.Result.Tasks.Count,
                AverageExecutionTimeMs: e.Result.Tasks.Count,
                Status: DetermineExecutionStatus(e.Result),
                WorkflowName: e.WorkflowContext?.WorkflowName ?? string.Empty,
                WorkflowDescription: e.WorkflowContext?.WorkflowDescription ?? string.Empty,
                IsArchived: e.IsArchived
            ))
            .ToList();

        var analytics = CalculateAnalytics(filtered);

        return Task.FromResult(new ExecutionQueryResult(
            Executions: executions,
            Analytics: analytics,
            Limit: pagination.Limit,
            Offset: pagination.Offset,
            TotalAvailable: totalAvailable));
    }

    private static bool MatchesFilter(ExecutionResultEntry entry, ExecutionQueryFilter? filter)
    {
        // Exclude archived entries by default unless caller opts in
        if (filter?.IncludeArchived != true && entry.IsArchived)
            return false;

        if (filter is null)
            return true;

        var status = DetermineExecutionStatus(entry.Result);

        if (!string.IsNullOrWhiteSpace(filter.Status) && !status.Equals(filter.Status, StringComparison.OrdinalIgnoreCase))
            return false;

        if (filter.StartDateUtc.HasValue && entry.CreatedAtUtc < filter.StartDateUtc)
            return false;

        if (filter.EndDateUtc.HasValue && entry.CreatedAtUtc > filter.EndDateUtc)
            return false;

        var taskCount = entry.Result.Tasks.Count;
        if (filter.MinTaskCount.HasValue && taskCount < filter.MinTaskCount)
            return false;

        if (filter.MaxTaskCount.HasValue && taskCount > filter.MaxTaskCount)
            return false;

        var avgTime = (long)entry.Result.Tasks.Count;
        if (filter.MinAverageExecutionTimeMs.HasValue && avgTime < filter.MinAverageExecutionTimeMs)
            return false;

        if (filter.MaxAverageExecutionTimeMs.HasValue && avgTime > filter.MaxAverageExecutionTimeMs)
            return false;

        return true;
    }

    private static string DetermineExecutionStatus(ExecutionResult result)
    {
        return result.Tasks.All(t => t.Status == TaskExecutionStatus.Succeeded)
            ? "Succeeded"
            : "PartiallyFailed";
    }

    private static ExecutionAnalytics CalculateAnalytics(List<ExecutionResultEntry> entries)
    {
        var totalCount = entries.Count;
        var successCount = entries.Count(e => DetermineExecutionStatus(e.Result) == "Succeeded");
        var failureCount = totalCount - successCount;
        var successRate = totalCount > 0 ? successCount / (double)totalCount : 0;
        var averageTaskCount = totalCount > 0 ? entries.Sum(e => e.Result.Tasks.Count) / totalCount : 0;
        var averageExecutionTime = totalCount > 0 ? entries.Sum(e => (long)e.Result.Tasks.Count) / totalCount : 0;

        return new ExecutionAnalytics(
            TotalCount: totalCount,
            SuccessCount: successCount,
            FailureCount: failureCount,
            SuccessRate: successRate,
            AverageTaskCount: averageTaskCount,
            AverageExecutionTimeMs: averageExecutionTime);
    }

    public Task ArchiveAsync(string executionId, Guid archivedBy, string? reason = null, int? retentionDays = null)
    {
        if (_store.TryGetValue(executionId, out var entry))
            _store[executionId] = entry with { IsArchived = true };
        return Task.CompletedTask;
    }

    public Task UnarchiveAsync(string executionId, Guid unarchivedBy)
    {
        if (_store.TryGetValue(executionId, out var entry))
            _store[executionId] = entry with { IsArchived = false };
        return Task.CompletedTask;
    }

    public Task PurgeAsync(string executionId, Guid purgedBy, string? reason = null)
    {
        _store.TryRemove(executionId, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ArchiveAuditEntry>> GetArchiveAuditAsync(string executionId)
        => Task.FromResult<IReadOnlyList<ArchiveAuditEntry>>(Array.Empty<ArchiveAuditEntry>());
}