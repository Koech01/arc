using Arc.Application.Results;
namespace Arc.Application.Execution;


public interface ITaskExecutionCache
{
    Task<TaskExecutionResult?> GetAsync(string taskHash);
    Task StoreAsync(string taskHash, TaskExecutionResult result, DateTime expiresAtUtc);
    Task InvalidateAsync(string? taskHash = null);

    /// <summary>Returns statistics about cache occupancy for admin inspection.</summary>
    Task<CacheStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Snapshot of task execution cache occupancy.</summary>
public sealed record CacheStats(
    int TotalEntries,
    int ExpiredEntries,
    int ActiveEntries,
    DateTime? OldestEntryUtc,
    DateTime? NewestEntryUtc
);