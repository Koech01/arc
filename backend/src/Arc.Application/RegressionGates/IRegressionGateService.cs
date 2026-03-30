using Arc.Domain.Models;
namespace Arc.Application.RegressionGates;


/// <summary>
/// Service for evaluating regression gates.
/// Compares candidate executions against golden baselines using divergence rules.
/// Deterministic: same inputs produce same test results.
/// </summary>
public interface IRegressionGateService
{
    /// <summary>
    /// Runs a regression gate test for a candidate execution.
    /// Compares candidate against the gate's golden execution baseline.
    /// Returns deterministic pass/fail result with detailed rule evaluations.
    /// </summary>
    Task<RegressionTestResult> RunGateAsync(
        RegressionGateId gateId,
        string candidateExecutionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs all active gates associated with a workflow against a candidate execution.
    /// Returns results for all gates that apply to the workflow.
    /// If no gates exist for the workflow, returns empty list.
    /// </summary>
    Task<IReadOnlyList<RegressionTestResult>> RunAllGatesForWorkflowAsync(
        string workflowId,
        string candidateExecutionId,
        CancellationToken cancellationToken = default);
}