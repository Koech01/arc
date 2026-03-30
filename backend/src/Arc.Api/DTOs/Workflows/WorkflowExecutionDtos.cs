using Arc.Api.DTOs;
namespace Arc.Api.DTOs.Workflows;

public sealed class WorkflowExecutionResponseDto
{
    public string ExecutionId { get; set; } = string.Empty;
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public TaskResultDto[] Tasks { get; set; } = Array.Empty<TaskResultDto>();
}