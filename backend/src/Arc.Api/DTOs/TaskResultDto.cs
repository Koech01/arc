namespace Arc.Api.DTOs;

public sealed record TaskResultDto(
    string TaskId,
    string TaskName,
    int ExecutionOrder,
    string Status
);