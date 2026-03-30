namespace Arc.Domain.Models;


/// <summary>
/// Summary of divergence between golden and candidate executions.
/// Computed deterministically from execution comparison results.
/// </summary>
public sealed class DivergenceSummary
{
    public int TotalRulesEvaluated { get; }
    public int RulesFailed { get; }
    public int DivergencesDetected { get; }

    // Legacy properties for infrastructure compatibility
    public double SimilarityPercentage { get; }
    public int IdenticalTaskCount { get; }
    public int DifferentTaskCount { get; }
    public int? DivergencePointIndex { get; }
    public IReadOnlyList<string> CriticalPathTaskIds { get; }

    public DivergenceSummary(int totalRulesEvaluated, int rulesFailed, int divergencesDetected)
    {
        TotalRulesEvaluated = totalRulesEvaluated;
        RulesFailed = rulesFailed;
        DivergencesDetected = divergencesDetected;
    }

    // Legacy constructor for infrastructure compatibility
    public DivergenceSummary(
        double similarityPercentage,
        int identicalTaskCount,
        int differentTaskCount,
        int? divergencePointIndex,
        IEnumerable<string> criticalPathTaskIds)
    {
        SimilarityPercentage = similarityPercentage;
        IdenticalTaskCount = identicalTaskCount;
        DifferentTaskCount = differentTaskCount;
        DivergencePointIndex = divergencePointIndex;
        CriticalPathTaskIds = (criticalPathTaskIds?.ToList() ?? new List<string>()).AsReadOnly();
    }
}