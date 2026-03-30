namespace Arc.Api.DTOs.RegressionGates;


/// <summary>
/// Request DTO for creating a regression gate.
/// </summary>
public sealed class CreateRegressionGateRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WorkflowId { get; set; }
    public string GoldenExecutionId { get; set; } = string.Empty;
    public List<DivergenceRuleDto> Rules { get; set; } = new();
}

/// <summary>
/// Request DTO for toggling gate active status.
/// </summary>
public sealed class ToggleRegressionGateRequestDto
{
    public bool IsActive { get; set; }
}

/// <summary>
/// Request DTO for running a gate test.
/// </summary>
public sealed class RunGateTestRequestDto
{
    public string CandidateExecutionId { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for marking execution as golden.
/// </summary>
public sealed class MarkGoldenRequestDto
{
    public string? Label { get; set; }
}

/// <summary>
/// Response DTO for regression gate.
/// </summary>
public sealed class RegressionGateResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WorkflowId { get; set; }
    public string GoldenExecutionId { get; set; } = string.Empty;
    public List<DivergenceRuleDto> Rules { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for divergence rule.
/// </summary>
public sealed class DivergenceRuleDto
{
    public string Type { get; set; } = string.Empty;
    public double Threshold { get; set; }
}

/// <summary>
/// Response DTO for regression test result.
/// </summary>
public sealed class RegressionTestResultDto
{
    public string GateId { get; set; } = string.Empty;
    public string GateName { get; set; } = string.Empty;
    public string CandidateExecutionId { get; set; } = string.Empty;
    public string GoldenExecutionId { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public List<RuleEvaluationResultDto> RuleResults { get; set; } = new();
    public DivergenceSummaryDto DivergenceSummary { get; set; } = new();
    public DateTime TestedAt { get; set; }
}

/// <summary>
/// DTO for rule evaluation result.
/// </summary>
public sealed class RuleEvaluationResultDto
{
    public string RuleType { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public double ActualValue { get; set; }
    public double Threshold { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// DTO for divergence summary.
/// </summary>
public sealed class DivergenceSummaryDto
{
    public double SimilarityPercentage { get; set; }
    public int IdenticalTaskCount { get; set; }
    public int DifferentTaskCount { get; set; }
    public int? DivergencePointIndex { get; set; }
    public List<string> CriticalPathTaskIds { get; set; } = new();
}

/// <summary>
/// Response DTO for golden execution metadata.
/// </summary>
public sealed class GoldenExecutionMetadataDto
{
    public string ExecutionId { get; set; } = string.Empty;
    public string? Label { get; set; }
    public DateTime MarkedAt { get; set; }
}