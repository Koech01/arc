using Arc.Domain.Models;
namespace Arc.Application.RegressionGates;


/// <summary>
/// Store for managing golden execution metadata.
/// Golden executions serve as baselines for regression gate testing.
/// </summary>
public interface IGoldenExecutionStore
{
    /// <summary>
    /// Marks an execution as golden (baseline) with optional label.
    /// </summary>
    Task MarkAsGoldenAsync(string executionId, string? label, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an execution is marked as golden.
    /// </summary>
    Task<bool> IsGoldenAsync(string executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all golden executions owned by a user.
    /// Returns metadata ordered by marked date descending (newest first).
    /// </summary>
    Task<IReadOnlyList<GoldenExecutionMetadata>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unmarks an execution as golden.
    /// Returns true if unmarked, false if not found.
    /// </summary>
    Task<bool> UnmarkAsGoldenAsync(string executionId, CancellationToken cancellationToken = default);
}