namespace Arc.Api.DTOs.Execution;


/// <summary>
/// Simple execution list item for the UI.
/// Contains workflowName, workflowDescription, and tasks as required by the frontend.
/// </summary>
public sealed class ExecutionListItemResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public string Duration { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string WorkflowDescription { get; set; } = string.Empty;
    public IReadOnlyList<TaskSummaryDto> Tasks { get; set; } = Array.Empty<TaskSummaryDto>();
    public bool IsArchived { get; set; }
}

/// <summary>
/// Concise task representation for inline list and detail responses.
/// Carries the minimum fields the UI needs to render an execution's task list.
/// </summary>
public sealed class TaskSummaryDto
{
    public string TaskId { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Output { get; set; }
}