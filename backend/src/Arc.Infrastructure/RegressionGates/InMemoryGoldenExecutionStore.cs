using Arc.Domain.Models;
using System.Collections.Concurrent;
using Arc.Application.RegressionGates;
namespace Arc.Infrastructure.RegressionGates;


/// <summary>
/// In-memory golden execution store for lightweight tests and local helpers.
/// </summary>
public sealed class InMemoryGoldenExecutionStore : IGoldenExecutionStore
{
    private readonly ConcurrentDictionary<string, GoldenExecutionMetadata> _store = new();

    public Task MarkAsGoldenAsync(string executionId, string? label, CancellationToken cancellationToken = default)
    {
        _store[executionId] = new GoldenExecutionMetadata(executionId, UserId.Anonymous, label, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task<bool> IsGoldenAsync(string executionId, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.ContainsKey(executionId));

    public Task<IReadOnlyList<GoldenExecutionMetadata>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        // In-memory store doesn't track ownership per user; return all entries
        var list = _store.Values
            .OrderByDescending(g => g.MarkedAtUtc)
            .ToList();
        return Task.FromResult<IReadOnlyList<GoldenExecutionMetadata>>(list);
    }

    public Task<bool> UnmarkAsGoldenAsync(string executionId, CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove(executionId, out _);
        return Task.FromResult(removed);
    }
}
