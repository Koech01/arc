namespace Arc.Domain.Models;


/// <summary>
/// Result of running a regression gate test.
/// Deterministic: same gate + same executions = same result.
/// </summary>
public sealed class RegressionTestResult
{
    public RegressionGateId GateId { get; init; }
    public string GateName { get; init; }
    public string CandidateExecutionId { get; init; }
    public GoldenExecutionId GoldenExecutionId { get; init; }
    public bool Passed { get; init; }
    public IReadOnlyList<RuleEvaluationResult> RuleResults { get; init; }
    public DivergenceSummary DivergenceSummary { get; init; }
    public DateTime TestedAtUtc { get; init; }

    public RegressionTestResult(
        RegressionGateId gateId,
        string gateName,
        string candidateExecutionId,
        GoldenExecutionId goldenExecutionId,
        bool passed,
        IEnumerable<RuleEvaluationResult> ruleResults,
        DivergenceSummary divergenceSummary,
        DateTime? testedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(gateName))
            throw new ArgumentException("Gate name cannot be null or empty");

        if (string.IsNullOrWhiteSpace(candidateExecutionId))
            throw new ArgumentException("Candidate execution ID cannot be null or empty");

        var ruleList = ruleResults?.ToList() ?? new List<RuleEvaluationResult>();
        if (ruleList.Count == 0)
            throw new ArgumentException("At least one rule result is required");

        GateId = gateId;
        GateName = gateName.Trim();
        CandidateExecutionId = candidateExecutionId.Trim();
        GoldenExecutionId = goldenExecutionId;
        Passed = passed;
        RuleResults = ruleList.AsReadOnly();
        DivergenceSummary = divergenceSummary ?? throw new ArgumentNullException(nameof(divergenceSummary));
        TestedAtUtc = testedAtUtc ?? DateTime.UtcNow;
    }
}