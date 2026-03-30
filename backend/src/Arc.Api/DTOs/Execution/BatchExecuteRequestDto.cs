namespace Arc.Api.DTOs.Execution;


/// <summary>
/// Individual execution item in batch request.
/// </summary>
public sealed record BatchExecutionRequestItem(string Input);

/// <summary>
/// Batch execution request DTO.
/// </summary>
public sealed record BatchExecuteRequestDto(
    IReadOnlyCollection<BatchExecutionRequestItem> Executions
);