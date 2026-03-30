using Arc.Domain.Exceptions;
namespace Arc.Domain.Models;


/// <summary>
/// Regression gate entity defining baseline execution and divergence rules.
/// Enforces domain invariants for safe change management workflows.
/// </summary>
public sealed class RegressionGate
{
    public RegressionGateId Id { get; init; }
    public UserId OwnerId { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public string? WorkflowId { get; init; }
    public GoldenExecutionId GoldenExecutionId { get; init; }
    public IReadOnlyList<DivergenceRule> Rules { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }

    public RegressionGate(
        RegressionGateId id,
        UserId ownerId,
        string name,
        GoldenExecutionId goldenExecutionId,
        IEnumerable<DivergenceRule> rules,
        string? description = null,
        string? workflowId = null,
        bool isActive = true,
        DateTime? createdAtUtc = null)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
            throw new RegressionGateInvalidException("Gate name cannot be null or empty.");

        if (name.Length > 200)
            throw new RegressionGateInvalidException("Gate name cannot exceed 200 characters.");

        // Validate description
        if (description != null && description.Length > 1000)
            throw new RegressionGateInvalidException("Gate description cannot exceed 1000 characters.");

        // Validate rules
        var ruleList = rules?.ToList() ?? new List<DivergenceRule>();
        if (ruleList.Count == 0)
            throw new RegressionGateInvalidException("Gate must have at least one divergence rule.");

        Id = id;
        OwnerId = ownerId;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        WorkflowId = string.IsNullOrWhiteSpace(workflowId) ? null : workflowId.Trim();
        GoldenExecutionId = goldenExecutionId;
        Rules = ruleList.AsReadOnly();
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Returns a new gate with updated IsActive status.
    /// </summary>
    public RegressionGate WithIsActive(bool isActive)
    {
        return new RegressionGate(
            Id,
            OwnerId,
            Name,
            GoldenExecutionId,
            Rules,
            Description,
            WorkflowId,
            isActive,
            CreatedAtUtc
        );
    }
}

public sealed class RegressionGateInvalidException : DomainException
{
    public RegressionGateInvalidException(string message) : base(message) { }
}