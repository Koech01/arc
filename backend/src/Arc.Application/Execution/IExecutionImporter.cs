using Arc.Application.Results;
namespace Arc.Application.Execution;

/// <summary>
/// Data structure for importing execution data from JSON.
/// Used when importing previously exported executions.
/// </summary>
public sealed class ExecutionImportData
{
    /// <summary>
    /// The execution metadata (ID, user, creation date, etc.)
    /// </summary>
    public required string ExecutionId { get; init; }

    /// <summary>
    /// The user ID who owns this execution.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Tasks that were executed, in execution order.
    /// </summary>
    public required IReadOnlyList<ImportedTaskData> Tasks { get; init; }

    /// <summary>
    /// Execution status: Succeeded, Failed
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// When the execution was created (ISO 8601)
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Audit log entries for this execution, ordered by sequence.
    /// Optional during import.
    /// </summary>
    public IReadOnlyList<ImportedAuditLogEntry>? AuditLogs { get; init; }
}

/// <summary>
/// Data structure for a single task imported from JSON.
/// </summary>
public sealed class ImportedTaskData
{
    public required string TaskId { get; init; }
    public required string TaskName { get; init; }
    public required int ExecutionOrder { get; init; }
    public required string Status { get; init; }
    public required string Output { get; init; }
}

/// <summary>
/// Data structure for audit log entry imported from JSON.
/// </summary>
public sealed class ImportedAuditLogEntry
{
    public required long Sequence { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public required string EventType { get; init; }
    public string? TaskId { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Service for deterministically importing execution data from various formats.
/// The importingUserId is always the authenticated caller - the JSON payload must not
/// supply or override the user identity (security invariant).
/// </summary>
public interface IExecutionImporter
{
    /// <summary>
    /// Imports a single execution from JSON.
    /// Supports both the v1.0 structured format and the legacy flat format.
    /// </summary>
    /// <param name="jsonData">Complete JSON export data (single execution).</param>
    /// <param name="importingUserId">Authenticated caller's user ID. Replaces any userId in the payload.</param>
    /// <returns>ExecutionResult of the imported execution, or null if validation fails.</returns>
    Task<ExecutionResult?> ImportFromJsonAsync(string jsonData, Guid importingUserId);

    /// <summary>
    /// Imports multiple executions from a JSON array.
    /// Processes each execution independently; failures do not halt the batch.
    /// </summary>
    /// <param name="jsonArrayData">JSON array of execution payloads.</param>
    /// <param name="importingUserId">Authenticated caller's user ID. Applied to all items.</param>
    /// <returns>List of import results with ExecutionId and success indicator.</returns>
    Task<IReadOnlyList<ExecutionImportResult>> ImportBulkFromJsonAsync(string jsonArrayData, Guid importingUserId);

    /// <summary>
    /// Validates JSON import data before committing to storage.
    /// Does not store; only validates structure and data integrity.
    /// </summary>
    /// <param name="jsonData">JSON data to validate.</param>
    /// <returns>Validation result with error messages if invalid.</returns>
    Task<ValidationResult> ValidateJsonImportAsync(string jsonData);
}

/// <summary>
/// Result of a single execution import operation.
/// </summary>
public sealed record ExecutionImportResult(
    string ExecutionId,
    bool Success,
    string? ErrorMessage = null
);

/// <summary>
/// Validation result for import operations.
/// </summary>
public sealed record ValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors
)
{
    public static ValidationResult Valid() => new(true, Array.Empty<string>());

    public static ValidationResult Invalid(params string[] errors) => new(false, errors);
}