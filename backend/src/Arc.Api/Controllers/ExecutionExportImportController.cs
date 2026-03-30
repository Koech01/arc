using Arc.Api.DTOs.Execution;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;
using Arc.Application.Identity;
using Microsoft.AspNetCore.Authorization;


namespace Arc.Api.Controllers;
/// <summary>
/// Export and import endpoints for execution data.
/// Enables downloading, backing up, and restoring execution history.
/// All operations preserve determinism and user ownership.
/// </summary>
[ApiController]
[Route("api/executions")]
[Authorize]
public sealed class ExecutionExportImportController : ControllerBase
{
    private readonly IExecutionExporter _exporter;
    private readonly IExecutionImporter _importer;
    private readonly IExecutionResultStore _resultStore;
    private readonly IUserContext _userContext;

    public ExecutionExportImportController(
        IExecutionExporter exporter,
        IExecutionImporter importer,
        IExecutionResultStore resultStore,
        IUserContext userContext)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _importer = importer ?? throw new ArgumentNullException(nameof(importer));
        _resultStore = resultStore ?? throw new ArgumentNullException(nameof(resultStore));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    /// <summary>
    /// Exports a single execution as JSON or CSV.
    /// Returns the complete execution data including all tasks and audit logs.
    /// </summary>
    [HttpGet("{executionId}/export")]
    [Produces("application/json", "text/csv")]
    public async Task<IActionResult> ExportExecution(string executionId, [FromQuery] string format = "json")
    {
        if (string.IsNullOrWhiteSpace(format))
            format = "json";

        format = format.ToLowerInvariant();
        if (format != "json" && format != "csv")
            return BadRequest(new { message = "Format must be 'json' or 'csv'", code = "INVALID_FORMAT" });

        try
        {
            string? exportData;
            string contentType;
            string filename;

            if (format == "json")
            {
                exportData = await _exporter.ExportAsJsonAsync(executionId);
                contentType = "application/json";
                filename = $"execution-{executionId}.json";
            }
            else
            {
                exportData = await _exporter.ExportAsCSVAsync(executionId);
                contentType = "text/csv";
                filename = $"execution-{executionId}.csv";
            }

            if (exportData == null)
                return NotFound(new { message = $"Execution '{executionId}' not found", code = "NOT_FOUND" });

            return File(System.Text.Encoding.UTF8.GetBytes(exportData), contentType, filename);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Export failed", details = ex.Message, code = "EXPORT_ERROR" });
        }
    }

    /// <summary>
    /// Exports multiple executions as JSON or CSV with filtering and pagination.
    /// </summary>
    [HttpPost("export-bulk")]
    [Produces("application/json", "text/csv")]
    public async Task<IActionResult> ExportBulk([FromBody] ExecutionExportBulkRequestDto requestDto)
    {
        var format = requestDto.Format?.ToLowerInvariant() ?? "json";
        if (format != "json" && format != "csv")
            return BadRequest(new { message = "Format must be 'json' or 'csv'", code = "INVALID_FORMAT" });

        try
        {
            DateTime? startDate = null;
            DateTime? endDate = null;

            if (!string.IsNullOrWhiteSpace(requestDto.StartDate))
            {
                if (!DateTime.TryParse(requestDto.StartDate, out var parsed))
                    return BadRequest(new { message = "Invalid startDate format", code = "BAD_REQUEST" });
                startDate = parsed.ToUniversalTime();
            }

            if (!string.IsNullOrWhiteSpace(requestDto.EndDate))
            {
                if (!DateTime.TryParse(requestDto.EndDate, out var parsed))
                    return BadRequest(new { message = "Invalid endDate format", code = "BAD_REQUEST" });
                endDate = parsed.ToUniversalTime();
            }

            var filter = new ExecutionQueryFilter(
                requestDto.Status, startDate, endDate,
                requestDto.MinTaskCount, requestDto.MaxTaskCount,
                requestDto.MinExecutionTimeMs, requestDto.MaxExecutionTimeMs,
                false);

            var pagination = Application.Execution.PaginationParams.Validate(requestDto.Limit, requestDto.Offset);

            string exportData;
            string contentType;
            string filename;

            if (format == "json")
            {
                exportData = await _exporter.ExportBulkAsJsonAsync(filter, pagination, _userContext.CurrentUserId.Value);
                contentType = "application/json";
                filename = $"executions-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            }
            else
            {
                exportData = await _exporter.ExportBulkAsCSVAsync(filter, pagination, _userContext.CurrentUserId.Value);
                contentType = "text/csv";
                filename = $"executions-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            }

            if (string.IsNullOrEmpty(exportData))
                return Ok(new { message = "No executions found matching filter", code = "EMPTY_RESULT" });

            return File(System.Text.Encoding.UTF8.GetBytes(exportData), contentType, filename);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Bulk export failed", details = ex.Message, code = "EXPORT_ERROR" });
        }
    }

    /// <summary>
    /// Exports audit logs for a specific execution in JSON format.
    /// </summary>
    [HttpGet("{executionId}/export/audit-logs")]
    public async Task<IActionResult> ExportAuditLogs(string executionId)
    {
        try
        {
            var auditData = await _exporter.ExportAuditLogsAsJsonAsync(executionId);
            if (auditData == null)
                return NotFound(new { message = $"Execution '{executionId}' not found", code = "NOT_FOUND" });

            return File(
                System.Text.Encoding.UTF8.GetBytes(auditData),
                "application/json",
                $"audit-logs-{executionId}.json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Audit export failed", details = ex.Message, code = "EXPORT_ERROR" });
        }
    }

    /// <summary>
    /// Validates import data before committing to storage.
    /// </summary>
    [HttpPost("import/validate")]
    public async Task<IActionResult> ValidateImport([FromBody] ExecutionImportRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Data))
            return BadRequest(new { message = "Data is required", code = "VALIDATION_ERROR" });

        try
        {
            var result = await _importer.ValidateJsonImportAsync(requestDto.Data);

            var response = new ExecutionImportValidationResponseDto
            {
                Valid = result.IsValid,
                Errors = result.Errors.ToList(),
                ValidatedAtUtc = DateTime.UtcNow,
                ExecutionCount = result.IsValid ? 1 : 0
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Validation failed", details = ex.Message, code = "VALIDATION_ERROR" });
        }
    }

    /// <summary>
    /// Imports a single execution from JSON data.
    /// The authenticated caller becomes the owner; any userId in the payload is ignored.
    /// </summary>
    [HttpPost("{executionId}/import")]
    public async Task<IActionResult> ImportExecution(
        string executionId,
        [FromBody] ExecutionImportRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Data))
            return BadRequest(new { message = "Data is required", code = "VALIDATION_ERROR" });

        try
        {
            string? payloadExecutionId = null;
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(requestDto.Data);
                var root = document.RootElement;

                // Support both v1.0 (execution.id) and legacy (executionId) formats
                if (root.TryGetProperty("execution", out var execEl) &&
                    execEl.TryGetProperty("id", out var idV1))
                {
                    payloadExecutionId = idV1.GetString();
                }
                else if (root.TryGetProperty("executionId", out var idLegacy))
                {
                    payloadExecutionId = idLegacy.GetString();
                }
            }
            catch (System.Text.Json.JsonException)
            {
                return BadRequest(new { message = "Import data is not valid JSON", code = "VALIDATION_ERROR" });
            }

            if (string.IsNullOrWhiteSpace(payloadExecutionId))
                return BadRequest(new { message = "ExecutionId is required in import payload", code = "VALIDATION_ERROR" });

            if (!string.Equals(executionId, payloadExecutionId, StringComparison.Ordinal))
                return BadRequest(new { message = "ExecutionId in route does not match payload", code = "EXECUTION_ID_MISMATCH" });

            var existing = await _resultStore.GetAsync(payloadExecutionId);
            if (existing != null && !requestDto.Overwrite)
                return Conflict(new
                {
                    message = $"Execution '{payloadExecutionId}' already exists. Set overwrite=true to replace.",
                    code = "CONFLICT"
                });

            var importingUserId = _userContext.CurrentUserId.Value;
            var result = await _importer.ImportFromJsonAsync(requestDto.Data, importingUserId);

            if (result == null)
                return BadRequest(new { message = "Import data is invalid or failed validation", code = "VALIDATION_ERROR" });

            return Created(
                $"/api/executions/{payloadExecutionId}",
                new ExecutionImportResultDto
                {
                    ExecutionId = payloadExecutionId,
                    Success = true,
                    ImportedAtUtc = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Import failed", details = ex.Message, code = "IMPORT_ERROR" });
        }
    }

    /// <summary>
    /// Imports multiple executions from JSON data.
    /// Processes each execution independently; failures do not halt the batch.
    /// The authenticated caller becomes the owner of all imported executions.
    /// </summary>
    [HttpPost("import-bulk")]
    public async Task<IActionResult> ImportBulk([FromBody] ExecutionImportRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Data))
            return BadRequest(new { message = "Data is required", code = "VALIDATION_ERROR" });

        try
        {
            var importingUserId = _userContext.CurrentUserId.Value;
            var importResults = await _importer.ImportBulkFromJsonAsync(requestDto.Data, importingUserId);

            var successCount = importResults.Count(r => r.Success);
            var failureCount = importResults.Count(r => !r.Success);

            var response = new ExecutionImportBulkResponseDto
            {
                Success = failureCount == 0,
                TotalCount = importResults.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                Results = importResults.Select(r => new ExecutionImportResultDto
                {
                    ExecutionId = r.ExecutionId,
                    Success = r.Success,
                    ErrorMessage = r.ErrorMessage,
                    ImportedAtUtc = DateTime.UtcNow
                }).ToList(),
                ImportedAtUtc = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Bulk import failed", details = ex.Message, code = "IMPORT_ERROR" });
        }
    }

    /// <summary>
    /// Frontend-compatible import endpoint.
    /// Accepts a JSON string in "jsonContent" or "data" field containing either
    /// a single execution object or a JSON array of executions.
    /// Handles both the v1.0 structured format and the legacy flat format.
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportFrontend([FromBody] ImportRequestDto requestDto)
    {
        var content = requestDto.ResolvedContent;
        if (string.IsNullOrWhiteSpace(content))
            return BadRequest(new { message = "jsonContent or data is required", code = "VALIDATION_ERROR" });

        try
        {
            var importingUserId = _userContext.CurrentUserId.Value;

            // Normalise: wrap a single object in an array so ImportBulkFromJsonAsync always gets an array
            var trimmed = content.TrimStart();
            string arrayJson;
            if (trimmed.StartsWith('['))
            {
                arrayJson = content;
            }
            else if (trimmed.StartsWith('{'))
            {
                arrayJson = $"[{content}]";
            }
            else
            {
                return BadRequest(new { message = "Import content must be a JSON object or array", code = "VALIDATION_ERROR" });
            }

            var importResults = await _importer.ImportBulkFromJsonAsync(arrayJson, importingUserId);

            var results = importResults.Select(r => new ImportResultDto
            {
                ExecutionId = r.ExecutionId,
                Success = r.Success,
                Error = r.ErrorMessage
            }).ToList();

            return Ok(new ImportResponseDto
            {
                Results = results,
                TotalImported = results.Count(r => r.Success),
                TotalFailed = results.Count(r => !r.Success),
                ImportedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Import failed", details = ex.Message, code = "IMPORT_ERROR" });
        }
    }
}