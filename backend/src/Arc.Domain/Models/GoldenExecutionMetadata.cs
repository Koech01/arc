namespace Arc.Domain.Models;


/// <summary>
/// Metadata for a golden (baseline) execution.
/// Used for regression gate baseline references.
/// </summary>
public sealed class GoldenExecutionMetadata
{
    public string ExecutionId { get; }
    public UserId MarkedByUserId { get; }
    public DateTime MarkedAt { get; }
    public string? Notes { get; }

    // Legacy properties for infrastructure compatibility
    public string? Label { get; }
    public UserId OwnerId { get; }
    public DateTime MarkedAtUtc { get; }

    public GoldenExecutionMetadata(string executionId, UserId markedByUserId, DateTime markedAt, string? notes)
    {
        ExecutionId = executionId;
        MarkedByUserId = markedByUserId;
        MarkedAt = markedAt;
        Notes = notes;
    }

    // Legacy constructor for infrastructure compatibility
    public GoldenExecutionMetadata(string executionId, UserId ownerId, string? label, DateTime markedAtUtc)
    {
        ExecutionId = executionId;
        OwnerId = ownerId;
        Label = label;
        MarkedAtUtc = markedAtUtc;
    }
}