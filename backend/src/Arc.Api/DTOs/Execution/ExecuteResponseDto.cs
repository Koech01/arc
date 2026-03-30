namespace Arc.Api.DTOs;

public sealed record ExecuteResponseDto(
    string ExecutionId,
    IReadOnlyCollection<TaskResultDto> Tasks
);