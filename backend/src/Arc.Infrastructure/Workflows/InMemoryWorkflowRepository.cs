using Arc.Domain.Models;
using System.Collections.Concurrent;
using Arc.Application.Workflows;
namespace Arc.Infrastructure.Workflows;


/// <summary>
/// In-memory workflow repository for lightweight tests and local helpers.
/// </summary>
public sealed class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly ConcurrentDictionary<string, Workflow> _store = new();

    public Task<Workflow> CreateAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        _store[workflow.Id] = workflow;
        return Task.FromResult(workflow);
    }

    public Task<Workflow?> GetByIdAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(workflowId, out var workflow);
        return Task.FromResult(workflow);
    }

    public Task<Workflow?> GetByNameAsync(string name, UserId userId, CancellationToken cancellationToken = default)
    {
        var workflow = _store.Values
            .FirstOrDefault(w => w.Name == name && w.CreatedBy == userId);
        return Task.FromResult(workflow);
    }

    public Task<IReadOnlyList<Workflow>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var workflows = _store.Values
            .Where(w => w.CreatedBy == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<Workflow>>(workflows);
    }

    public Task<bool> DeleteAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove(workflowId, out _);
        return Task.FromResult(removed);
    }
}
