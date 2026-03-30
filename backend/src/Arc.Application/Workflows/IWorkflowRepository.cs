using Arc.Domain.Models;
namespace Arc.Application.Workflows;


public interface IWorkflowRepository
{
    Task<Workflow> CreateAsync(Workflow workflow, CancellationToken cancellationToken = default);
    Task<Workflow?> GetByIdAsync(string workflowId, CancellationToken cancellationToken = default);
    Task<Workflow?> GetByNameAsync(string name, UserId userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workflow>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string workflowId, CancellationToken cancellationToken = default);
}