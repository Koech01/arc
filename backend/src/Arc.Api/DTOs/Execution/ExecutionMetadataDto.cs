namespace Arc.Api.DTOs.Execution;


public sealed class ExecutionMetadataDto
{
    public string ExecutionId { get; set; } = string.Empty;
    public string? WorkflowId { get; set; }
    public string? WorkflowVersion { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
    public string Environment { get; set; } = "production";
    public int TotalTasks { get; set; }
    public int SuccessfulTasks { get; set; }
    public int FailedTasks { get; set; }
}