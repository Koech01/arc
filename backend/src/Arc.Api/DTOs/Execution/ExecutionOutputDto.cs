namespace Arc.Api.DTOs.Execution;

public sealed class ExecutionOutputDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = "text/plain";
}