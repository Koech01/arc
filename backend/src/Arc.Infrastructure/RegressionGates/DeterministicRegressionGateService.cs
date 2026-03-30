using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;
using Microsoft.Extensions.Logging;
using Arc.Application.RegressionGates;
namespace Arc.Infrastructure.RegressionGates;


/// <summary>
/// Deterministic regression gate service.
/// Evaluates candidate executions against golden baselines using divergence rules.
/// Same inputs always produce same results.
/// </summary>
public sealed class DeterministicRegressionGateService : IRegressionGateService
{
    private readonly IRegressionGateRepository _gateRepository;
    private readonly IExecutionResultStore _executionStore;
    private readonly IExecutionComparer _comparer;
    private readonly IExecutionProfiler _profiler;
    private readonly ILogger<DeterministicRegressionGateService> _logger;

    public DeterministicRegressionGateService(
        IRegressionGateRepository gateRepository,
        IExecutionResultStore executionStore,
        IExecutionComparer comparer,
        IExecutionProfiler profiler,
        ILogger<DeterministicRegressionGateService> logger)
    {
        _gateRepository = gateRepository ?? throw new ArgumentNullException(nameof(gateRepository));
        _executionStore = executionStore ?? throw new ArgumentNullException(nameof(executionStore));
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RegressionTestResult> RunGateAsync(
        RegressionGateId gateId,
        string candidateExecutionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running regression gate {GateId} for candidate execution {CandidateId}", 
            gateId, candidateExecutionId);

        // Retrieve gate
        var gate = await _gateRepository.GetByIdAsync(gateId, cancellationToken);
        if (gate == null)
            throw new RegressionGateInvalidException($"Regression gate {gateId} not found");

        if (!gate.IsActive)
            throw new RegressionGateInvalidException($"Regression gate {gateId} is not active");

        // Verify both executions exist
        var goldenExecution = await _executionStore.GetAsync(gate.GoldenExecutionId.Value);
        if (goldenExecution == null)
            throw new RegressionGateInvalidException($"Golden execution {gate.GoldenExecutionId} not found");

        var candidateExecution = await _executionStore.GetAsync(candidateExecutionId);
        if (candidateExecution == null)
            throw new RegressionGateInvalidException($"Candidate execution {candidateExecutionId} not found");

        // If golden and candidate are the same execution, they are identical by definition
        if (gate.GoldenExecutionId.Value == candidateExecutionId)
        {
            var identicalSummary = new DivergenceSummary(1.0, goldenExecution.Tasks.Count, 0, -1, new List<string>());
            var identicalRules = gate.Rules.Select(r => new RuleEvaluationResult(r.Type, true, 1.0, r.Threshold, "Identical execution")).ToList();
            return new RegressionTestResult(gate.Id, gate.Name, candidateExecutionId, gate.GoldenExecutionId, true, identicalRules, identicalSummary);
        }

        // Compare executions
        var comparison = await _comparer.CompareAsync(
            gate.GoldenExecutionId.Value, 
            candidateExecutionId);
        
        if (comparison == null)
            throw new InvalidOperationException("Failed to compare executions");

        // Get critical path data for both executions (may be null if no audit logs)
        var goldenProfile = await _profiler.GenerateProfileAsync(gate.GoldenExecutionId.Value);
        var candidateProfile = await _profiler.GenerateProfileAsync(candidateExecutionId);

        // Build divergence summary
        var divergenceSummary = new DivergenceSummary(
            comparison.Metrics.SimilarityPercentage,
            comparison.Metrics.IdenticalTasks,
            comparison.Metrics.DifferentTasks,
            comparison.Metrics.DivergencePointIndex,
            goldenProfile?.CriticalPath.CriticalPathTaskIds.ToList() ?? new List<string>()
        );

        // Evaluate each rule
        var ruleResults = new List<RuleEvaluationResult>();
        foreach (var rule in gate.Rules)
        {
            var ruleResult = EvaluateRule(rule, comparison, goldenProfile, candidateProfile, goldenExecution, candidateExecution);
            ruleResults.Add(ruleResult);
        }

        // Gate passes only if all rules pass
        var passed = ruleResults.All(r => r.Passed);

        var testResult = new RegressionTestResult(
            gate.Id,
            gate.Name,
            candidateExecutionId,
            gate.GoldenExecutionId,
            passed,
            ruleResults,
            divergenceSummary
        );

        _logger.LogInformation(
            "Regression gate {GateId} completed: {Result}. Similarity: {Similarity:P2}",
            gateId, 
            passed ? "PASSED" : "FAILED", 
            divergenceSummary.SimilarityPercentage);

        return testResult;
    }

    public async Task<IReadOnlyList<RegressionTestResult>> RunAllGatesForWorkflowAsync(
        string workflowId,
        string candidateExecutionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Running all regression gates for workflow {WorkflowId} against candidate {CandidateId}", 
            workflowId, 
            candidateExecutionId);

        var gates = await _gateRepository.ListByWorkflowAsync(workflowId, cancellationToken);

        if (gates.Count == 0)
        {
            _logger.LogInformation("No active gates found for workflow {WorkflowId}", workflowId);
            return Array.Empty<RegressionTestResult>();
        }

        var results = new List<RegressionTestResult>();
        foreach (var gate in gates)
        {
            try
            {
                var result = await RunGateAsync(gate.Id, candidateExecutionId, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run gate {GateId} for workflow {WorkflowId}", gate.Id, workflowId);
                // Continue with other gates even if one fails
            }
        }

        _logger.LogInformation(
            "Completed {TotalCount} gates for workflow {WorkflowId}. Passed: {PassedCount}, Failed: {FailedCount}",
            results.Count,
            workflowId,
            results.Count(r => r.Passed),
            results.Count(r => !r.Passed));

        return results;
    }

    private static RuleEvaluationResult EvaluateRule(
        DivergenceRule rule,
        ExecutionComparisonResult comparison,
        ExecutionPerformanceProfile? goldenProfile,
        ExecutionPerformanceProfile? candidateProfile,
        ExecutionResult goldenExecution,
        ExecutionResult candidateExecution)
    {
        return rule.Type switch
        {
            DivergenceRuleType.SimilarityPercentage => EvaluateSimilarityPercentage(rule, comparison),
            DivergenceRuleType.MaxTaskDivergence => EvaluateMaxTaskDivergence(rule, comparison),
            DivergenceRuleType.CriticalPathPreservation => EvaluateCriticalPathPreservation(rule, comparison, goldenProfile, candidateProfile),
            DivergenceRuleType.NoStatusDegradation => EvaluateNoStatusDegradation(rule, comparison, goldenExecution, candidateExecution),
            _ => throw new ArgumentOutOfRangeException(nameof(rule.Type), rule.Type, "Unknown rule type")
        };
    }

    private static RuleEvaluationResult EvaluateSimilarityPercentage(
        DivergenceRule rule,
        ExecutionComparisonResult comparison)
    {
        var actualValue = comparison.Metrics.SimilarityPercentage;
        var passed = actualValue >= rule.Threshold;

        var reason = passed
            ? $"Similarity {actualValue:P0} meets threshold {rule.Threshold:P0}"
            : $"Similarity {actualValue:P0} is below threshold {rule.Threshold:P0}";

        return new RuleEvaluationResult(rule.Type, passed, actualValue, rule.Threshold, reason);
    }

    private static RuleEvaluationResult EvaluateMaxTaskDivergence(
        DivergenceRule rule,
        ExecutionComparisonResult comparison)
    {
        // For max task divergence, we check if any task has a divergence > (1 - threshold)
        // If threshold is 0.9, then max allowed divergence per task is 0.1
        var maxAllowedDivergence = 1.0 - rule.Threshold;

        var taskDivergences = comparison.TaskComparisons
            .Where(tc => tc.IsDifferent)
            .Select(tc => 1.0) // Task is either identical (0) or different (1) in this simplified model
            .DefaultIfEmpty(0.0)
            .ToList();

        var maxActualDivergence = taskDivergences.Any() ? taskDivergences.Max() : 0.0;
        var passed = maxActualDivergence <= maxAllowedDivergence;

        var reason = passed
            ? $"No task exceeds divergence threshold {maxAllowedDivergence:P0}"
            : $"Task divergence {maxActualDivergence:P0} exceeds threshold {maxAllowedDivergence:P0}";

        return new RuleEvaluationResult(rule.Type, passed, maxActualDivergence, rule.Threshold, reason);
    }

    private static RuleEvaluationResult EvaluateCriticalPathPreservation(
        DivergenceRule rule,
        ExecutionComparisonResult comparison,
        ExecutionPerformanceProfile? goldenProfile,
        ExecutionPerformanceProfile? candidateProfile)
    {
        if (goldenProfile == null || candidateProfile == null)
            return new RuleEvaluationResult(rule.Type, true, 1.0, 1.0, "No profile data available; rule skipped");

        var goldenCriticalPathIds = goldenProfile.CriticalPath.CriticalPathTaskIds.ToHashSet();
        var candidateCriticalPathIds = candidateProfile.CriticalPath.CriticalPathTaskIds.ToHashSet();

        // Check if critical path tasks have identical outputs
        var criticalPathDifferences = comparison.TaskComparisons
            .Where(tc => goldenCriticalPathIds.Contains(tc.TaskId))
            .Count(tc => tc.IsDifferent);

        var totalCriticalPathTasks = goldenCriticalPathIds.Count;
        var actualValue = totalCriticalPathTasks == 0 
            ? 1.0 
            : 1.0 - ((double)criticalPathDifferences / totalCriticalPathTasks);

        var passed = criticalPathDifferences == 0;

        var reason = passed
            ? $"All {totalCriticalPathTasks} critical path tasks are identical"
            : $"{criticalPathDifferences} of {totalCriticalPathTasks} critical path tasks differ";

        return new RuleEvaluationResult(rule.Type, passed, actualValue, 1.0, reason);
    }

    private static RuleEvaluationResult EvaluateNoStatusDegradation(
        DivergenceRule rule,
        ExecutionComparisonResult comparison,
        ExecutionResult goldenExecution,
        ExecutionResult candidateExecution)
    {
        var degradedTasks = 0;

        foreach (var taskComparison in comparison.TaskComparisons)
        {
            var goldenTask = goldenExecution.Tasks.FirstOrDefault(t => t.TaskId == taskComparison.TaskId);
            var candidateTask = candidateExecution.Tasks.FirstOrDefault(t => t.TaskId == taskComparison.TaskId);

            if (goldenTask != null && candidateTask != null)
            {
                // Degradation = golden succeeded but candidate failed
                if (goldenTask.Status == TaskExecutionStatus.Succeeded && 
                    candidateTask.Status == TaskExecutionStatus.Failed)
                {
                    degradedTasks++;
                }
            }
        }

        var passed = degradedTasks == 0;
        var actualValue = passed ? 1.0 : 0.0;

        var reason = passed
            ? "No tasks degraded from Succeeded to Failed"
            : $"{degradedTasks} task(s) degraded from Succeeded to Failed";

        return new RuleEvaluationResult(rule.Type, passed, actualValue, 1.0, reason);
    }
}