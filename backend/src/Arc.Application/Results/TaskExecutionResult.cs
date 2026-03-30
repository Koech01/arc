namespace Arc.Application.Results;
using System.Text.Json.Serialization;


public sealed record TaskExecutionResult(
    [property: JsonPropertyName("taskId")] string TaskId,
    [property: JsonPropertyName("taskName")] string TaskName,
    [property: JsonPropertyName("executionOrder")] int ExecutionOrder,
    [property: JsonPropertyName("status")] TaskExecutionStatus Status,
    [property: JsonPropertyName("output")] string Output
);