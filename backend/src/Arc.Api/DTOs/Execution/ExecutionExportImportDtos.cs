namespace Arc.Api.DTOs.Execution;
using System.Text.Json.Serialization;


/// <summary>
/// Request DTO for batch export of executions.
/// Allows filtering which executions to export.
/// </summary>
public sealed class ExecutionExportBulkRequestDto
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public string? EndDate { get; set; }

    [JsonPropertyName("minTaskCount")]
    public int? MinTaskCount { get; set; }

    [JsonPropertyName("maxTaskCount")]
    public int? MaxTaskCount { get; set; }

    [JsonPropertyName("minExecutionTimeMs")]
    public int? MinExecutionTimeMs { get; set; }

    [JsonPropertyName("maxExecutionTimeMs")]
    public int? MaxExecutionTimeMs { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("offset")]
    public int? Offset { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; } = "json"; // "json" or "csv"
}

/// <summary>
/// Response DTO for execution export (single or bulk).
/// Contains the exported data in the requested format.
/// </summary>
public sealed class ExecutionExportResponseDto
{
    [JsonPropertyName("success")]
    public required bool Success { get; set; }

    [JsonPropertyName("format")]
    public required string Format { get; set; } // "json", "csv"

    [JsonPropertyName("contentType")]
    public required string ContentType { get; set; } // "application/json", "text/csv"

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("exportedAt")]
    public DateTime ExportedAtUtc { get; set; }
}

/// <summary>
/// Request DTO for importing execution data.
/// Contains the complete JSON export data.
/// </summary>
public sealed class ExecutionImportRequestDto
{
    [JsonPropertyName("data")]
    public required string Data { get; set; }

    [JsonPropertyName("overwrite")]
    public bool Overwrite { get; set; } = false; // If true, allows overwriting existing executions
}

/// <summary>
/// Response DTO for a single import operation.
/// </summary>
public sealed class ExecutionImportResultDto
{
    [JsonPropertyName("executionId")]
    public required string ExecutionId { get; set; }

    [JsonPropertyName("success")]
    public required bool Success { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("importedAt")]
    public DateTime ImportedAtUtc { get; set; }
}

/// <summary>
/// Response DTO for bulk import operation.
/// Contains results for each execution in the import.
/// </summary>
public sealed class ExecutionImportBulkResponseDto
{
    [JsonPropertyName("success")]
    public required bool Success { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }

    [JsonPropertyName("failureCount")]
    public int FailureCount { get; set; }

    [JsonPropertyName("results")]
    public required List<ExecutionImportResultDto> Results { get; set; }

    [JsonPropertyName("importedAt")]
    public DateTime ImportedAtUtc { get; set; }
}

// Frontend-compatible import DTOs 
// These match the shape the Arc frontend sends/expects for its import UI.

/// <summary>
/// Frontend-compatible import request. Accepts a JSON string in either
/// "jsonContent" (frontend UI) or "data" (API clients) field.
/// Handles a single execution object or a JSON array.
/// </summary>
public sealed class ImportRequestDto
{
    /// <summary>Raw JSON string containing one execution object or an array.</summary>
    [JsonPropertyName("jsonContent")]
    public string? JsonContent { get; set; }

    /// <summary>Alias accepted from API clients (same semantics as JsonContent).</summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>Returns whichever field was populated, preferring jsonContent.</summary>
    [JsonIgnore]
    public string? ResolvedContent => JsonContent ?? Data;
}

/// <summary>Per-execution result for the frontend import response.</summary>
public sealed class ImportResultDto
{
    [JsonPropertyName("executionId")]
    public required string ExecutionId { get; set; }

    [JsonPropertyName("success")]
    public required bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Frontend-compatible aggregate response for POST /api/executions/import.
/// </summary>
public sealed class ImportResponseDto
{
    [JsonPropertyName("results")]
    public required List<ImportResultDto> Results { get; set; }

    [JsonPropertyName("totalImported")]
    public int TotalImported { get; set; }

    [JsonPropertyName("totalFailed")]
    public int TotalFailed { get; set; }

    [JsonPropertyName("importedAt")]
    public DateTime ImportedAt { get; set; }
}

/// <summary>
/// Response DTO for validation of import data.
/// Used before actually importing to catch errors early.
/// </summary>
public sealed class ExecutionImportValidationResponseDto
{
    [JsonPropertyName("valid")]
    public required bool Valid { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();

    [JsonPropertyName("executionCount")]
    public int ExecutionCount { get; set; }

    [JsonPropertyName("validatedAt")]
    public DateTime ValidatedAtUtc { get; set; }
}