namespace Arc.Api.DTOs.Execution;


public sealed class ExecutionTaskDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long Duration { get; set; }
    public string AgentType { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
    public string? Output { get; set; }
    public string? Error { get; set; }
}