using Arc.Domain.Models;
using System.Collections.Concurrent;
using Arc.Application.RegressionGates;
namespace Arc.Infrastructure.RegressionGates;


/// <summary>
/// In-memory regression gate repository for SQLite/development fallback.
/// </summary>
public sealed class InMemoryRegressionGateRepository : IRegressionGateRepository
{
    private readonly ConcurrentDictionary<Guid, RegressionGate> _store = new();

    public Task<RegressionGate> CreateAsync(RegressionGate gate, CancellationToken cancellationToken = default)
    {
        _store[gate.Id.Value] = gate;
        return Task.FromResult(gate);
    }

    public Task<RegressionGate?> GetByIdAsync(RegressionGateId id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id.Value, out var gate);
        return Task.FromResult(gate);
    }

    public Task<IReadOnlyList<RegressionGate>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var gates = _store.Values
            .Where(g => g.OwnerId == userId)
            .OrderByDescending(g => g.CreatedAtUtc)
            .ToList();
        return Task.FromResult<IReadOnlyList<RegressionGate>>(gates);
    }

    public Task<IReadOnlyList<RegressionGate>> ListByWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var gates = _store.Values
            .Where(g => g.WorkflowId == workflowId && g.IsActive)
            .OrderByDescending(g => g.CreatedAtUtc)
            .ToList();
        return Task.FromResult<IReadOnlyList<RegressionGate>>(gates);
    }

    public Task<bool> DeleteAsync(RegressionGateId id, CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove(id.Value, out _);
        return Task.FromResult(removed);
    }

    public Task<bool> UpdateIsActiveAsync(RegressionGateId id, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(id.Value, out var gate))
            return Task.FromResult(false);

        _store[id.Value] = gate.WithIsActive(isActive);
        return Task.FromResult(true);
    }
}
