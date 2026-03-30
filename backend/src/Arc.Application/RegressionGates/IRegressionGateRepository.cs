using Arc.Domain.Models;
namespace Arc.Application.RegressionGates;


/// <summary>
/// Repository for managing regression gate persistence.
/// Follows Clean Architecture: interface in Application, implementation in Infrastructure.
/// </summary>
public interface IRegressionGateRepository
{
    /// <summary>
    /// Creates a new regression gate.
    /// </summary>
    Task<RegressionGate> CreateAsync(RegressionGate gate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a gate by ID.
    /// Returns null if not found.
    /// </summary>
    Task<RegressionGate?> GetByIdAsync(RegressionGateId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all gates owned by a user.
    /// Returns gates ordered by creation date descending (newest first).
    /// </summary>
    Task<IReadOnlyList<RegressionGate>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active gates associated with a specific workflow.
    /// Returns gates ordered by creation date descending.
    /// </summary>
    Task<IReadOnlyList<RegressionGate>> ListByWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a gate by ID.
    /// Returns true if deleted, false if not found.
    /// </summary>
    Task<bool> DeleteAsync(RegressionGateId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the IsActive status of a gate.
    /// Returns true if updated, false if not found.
    /// </summary>
    Task<bool> UpdateIsActiveAsync(RegressionGateId id, bool isActive, CancellationToken cancellationToken = default);
}