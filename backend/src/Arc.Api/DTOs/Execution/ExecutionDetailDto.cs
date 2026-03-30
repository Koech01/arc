namespace Arc.Api.DTOs.Execution;


/// <summary>
/// Detailed execution view including workflow context and task list.
/// All three fields required by the UI (workflowName, workflowDescription, tasks) are present.
/// </summary>
public sealed class ExecutionDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long Duration { get; set; }
    public string TriggerType { get; set; } = "Manual";
    public string? WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string WorkflowDescription { get; set; } = string.Empty;
    public IReadOnlyList<TaskSummaryDto> Tasks { get; set; } = Array.Empty<TaskSummaryDto>();
}