namespace Arc.Api.DTOs.Execution;


public sealed class ExecutionLogDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TaskId { get; set; }
}