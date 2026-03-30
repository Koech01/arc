using System.Text.Json;
using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.Telemetry;
namespace Arc.Infrastructure.Execution;


public sealed class DeterministicExecutionImporter : IExecutionImporter
{
    private readonly IExecutionResultStore _resultStore;
    private readonly IAuditLogger _auditLogger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DeterministicExecutionImporter(
        IExecutionResultStore resultStore,
        IAuditLogger auditLogger)
    {
        _resultStore = resultStore ?? throw new ArgumentNullException(nameof(resultStore));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    }

    // Public interface 

    public async Task<ExecutionResult?> ImportFromJsonAsync(string jsonData, Guid importingUserId)
    {
        try
        {
            var normalized = ParseAndNormalize(jsonData, importingUserId);
            if (normalized is null)
                return null;

            var validation = ValidateNormalized(normalized);
            if (!validation.IsValid)
                return null;

            var executionResult = BuildExecutionResult(normalized, importingUserId);
            var workflowContext = BuildWorkflowContext(normalized);

            await _resultStore.StoreAsync(
                normalized.ExecutionId,
                executionResult,
                normalized.CreatedAtUtc,
                workflowContext);

            await ImportAuditLogsAsync(normalized.ExecutionId, normalized.AuditTrail);

            return executionResult;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ExecutionImportResult>> ImportBulkFromJsonAsync(
        string jsonArrayData,
        Guid importingUserId)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<JsonElement>>(jsonArrayData, JsonOptions);
            if (items is null || items.Count == 0)
                return Array.Empty<ExecutionImportResult>();

            var results = new List<ExecutionImportResult>();

            foreach (var element in items)
            {
                var raw = element.GetRawText();
                try
                {
                    var normalized = ParseAndNormalize(raw, importingUserId);
                    if (normalized is null)
                    {
                        results.Add(new ExecutionImportResult("unknown", false, "Failed to parse import payload"));
                        continue;
                    }

                    var validation = ValidateNormalized(normalized);
                    if (!validation.IsValid)
                    {
                        results.Add(new ExecutionImportResult(
                            normalized.ExecutionId,
                            false,
                            $"Validation failed: {string.Join("; ", validation.Errors)}"));
                        continue;
                    }

                    var executionResult = BuildExecutionResult(normalized, importingUserId);
                    var workflowContext = BuildWorkflowContext(normalized);

                    await _resultStore.StoreAsync(
                        normalized.ExecutionId,
                        executionResult,
                        normalized.CreatedAtUtc,
                        workflowContext);

                    await ImportAuditLogsAsync(normalized.ExecutionId, normalized.AuditTrail);

                    results.Add(new ExecutionImportResult(normalized.ExecutionId, true));
                }
                catch (Exception ex)
                {
                    results.Add(new ExecutionImportResult("unknown", false, $"Import error: {ex.Message}"));
                }
            }

            return results;
        }
        catch
        {
            return Array.Empty<ExecutionImportResult>();
        }
    }

    public Task<ValidationResult> ValidateJsonImportAsync(string jsonData)
    {
        try
        {
            // Use a placeholder userId for validation-only path (not persisted)
            var normalized = ParseAndNormalize(jsonData, Guid.Empty);
            if (normalized is null)
                return Task.FromResult(ValidationResult.Invalid("Invalid JSON structure or unrecognized format"));

            return Task.FromResult(ValidateNormalized(normalized));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ValidationResult.Invalid($"JSON parsing error: {ex.Message}"));
        }
    }

    // Format detection & normalization 

    /// <summary>
    /// Parses the raw JSON into a canonical NormalizedImportPayload,
    /// dispatching between the v1.0 structured format and the legacy flat format.
    /// </summary>
    private static NormalizedImportPayload? ParseAndNormalize(string jsonData, Guid importingUserId)
    {
        using var doc = JsonDocument.Parse(jsonData);
        var root = doc.RootElement;

        if (root.TryGetProperty("schemaVersion", out _))
            return NormalizeV1(root, importingUserId);

        if (root.TryGetProperty("executionId", out _))
            return NormalizeLegacy(root, importingUserId);

        return null;
    }

    /// <summary>
    /// Normalizes the v1.0 structured format.
    /// { schemaVersion, workflow?, execution, auditTrail? }
    /// </summary>
    private static NormalizedImportPayload? NormalizeV1(JsonElement root, Guid importingUserId)
    {
        if (!root.TryGetProperty("execution", out var execEl))
            return null;

        if (!execEl.TryGetProperty("id", out var idEl))
            return null;

        var executionId = idEl.GetString();
        if (string.IsNullOrWhiteSpace(executionId))
            return null;

        var createdAtUtc = execEl.TryGetProperty("createdAtUtc", out var catEl) && catEl.TryGetDateTime(out var cat)
            ? DateTime.SpecifyKind(cat, DateTimeKind.Utc)
            : DateTime.UtcNow;

        var status = execEl.TryGetProperty("status", out var stEl) ? stEl.GetString() ?? "Succeeded" : "Succeeded";

        // Workflow context - optional
        string? workflowName = null;
        string? workflowDescription = null;

        if (root.TryGetProperty("workflow", out var wfEl))
        {
            workflowName = wfEl.TryGetProperty("name", out var wnEl) ? wnEl.GetString() : null;
            workflowDescription = wfEl.TryGetProperty("description", out var wdEl) ? wdEl.GetString() : null;
        }

        // Tasks
        var tasks = new List<NormalizedTaskPayload>();
        if (execEl.TryGetProperty("tasks", out var tasksEl) && tasksEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in tasksEl.EnumerateArray())
            {
                var taskId = t.TryGetProperty("taskId", out var tiEl) ? tiEl.GetString() : null;
                var taskName = t.TryGetProperty("taskName", out var tnEl) ? tnEl.GetString() : null;
                var order = t.TryGetProperty("executionOrder", out var eoEl) ? eoEl.GetInt32() : 0;
                var taskStatus = t.TryGetProperty("status", out var tsEl) ? tsEl.GetString() ?? "Succeeded" : "Succeeded";
                var output = ResolveOutput(t);

                tasks.Add(new NormalizedTaskPayload(
                    taskId ?? string.Empty,
                    taskName ?? string.Empty,
                    order,
                    NormalizeStatus(taskStatus),
                    output));
            }
        }

        // Audit trail
        var auditTrail = ParseAuditTrail(root, "auditTrail");

        return new NormalizedImportPayload(
            executionId,
            NormalizeStatus(status),
            createdAtUtc,
            workflowName,
            workflowDescription,
            tasks,
            auditTrail);
    }

    /// <summary>
    /// Normalizes the legacy flat format.
    /// { executionId, userId, status, tasks, auditLogs? }
    /// userId in payload is ignored; importingUserId is used instead.
    /// </summary>
    private static NormalizedImportPayload? NormalizeLegacy(JsonElement root, Guid importingUserId)
    {
        var executionId = root.TryGetProperty("executionId", out var eidEl) ? eidEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(executionId))
            return null;

        var createdAtUtc = root.TryGetProperty("createdAtUtc", out var catEl) && catEl.TryGetDateTime(out var cat)
            ? DateTime.SpecifyKind(cat, DateTimeKind.Utc)
            : DateTime.UtcNow;

        var status = root.TryGetProperty("status", out var stEl) ? stEl.GetString() ?? "Succeeded" : "Succeeded";

        var tasks = new List<NormalizedTaskPayload>();
        if (root.TryGetProperty("tasks", out var tasksEl) && tasksEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in tasksEl.EnumerateArray())
            {
                var taskId = t.TryGetProperty("taskId", out var tiEl) ? tiEl.GetString() : null;
                var taskName = t.TryGetProperty("taskName", out var tnEl) ? tnEl.GetString() : null;
                var order = t.TryGetProperty("executionOrder", out var eoEl) ? eoEl.GetInt32() : 0;
                var taskStatus = t.TryGetProperty("status", out var tsEl) ? tsEl.GetString() ?? "Succeeded" : "Succeeded";
                var output = ResolveOutput(t);

                tasks.Add(new NormalizedTaskPayload(
                    taskId ?? string.Empty,
                    taskName ?? string.Empty,
                    order,
                    NormalizeStatus(taskStatus),
                    output));
            }
        }

        var auditTrail = ParseAuditTrail(root, "auditLogs");

        return new NormalizedImportPayload(
            executionId,
            NormalizeStatus(status),
            createdAtUtc,
            WorkflowName: null,
            WorkflowDescription: null,
            tasks,
            auditTrail);
    }

    // Validation 

    private static ValidationResult ValidateNormalized(NormalizedImportPayload payload)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(payload.ExecutionId))
            errors.Add("ExecutionId is required and cannot be empty");

        if (payload.Tasks.Count == 0)
            errors.Add("At least one task is required");

        if (!Enum.TryParse<TaskExecutionStatus>(payload.Status, true, out _))
            errors.Add($"Invalid execution status: '{payload.Status}'. Valid values: Succeeded, Failed, Running");

        for (int i = 0; i < payload.Tasks.Count; i++)
        {
            var task = payload.Tasks[i];

            if (string.IsNullOrWhiteSpace(task.TaskId))
                errors.Add($"Task {i}: TaskId is required");

            if (string.IsNullOrWhiteSpace(task.TaskName))
                errors.Add($"Task {i}: TaskName is required");

            if (!Enum.TryParse<TaskExecutionStatus>(task.Status, true, out _))
                errors.Add($"Task {i}: Invalid status '{task.Status}'");
        }

        foreach (var log in payload.AuditTrail)
        {
            if (!Enum.TryParse<AuditEventType>(log.EventType, true, out _))
                errors.Add($"Audit entry seq={log.Sequence}: Invalid event type '{log.EventType}'");
        }

        return errors.Count > 0
            ? new ValidationResult(false, errors)
            : ValidationResult.Valid();
    }

    // Domain object construction

    private static ExecutionResult BuildExecutionResult(NormalizedImportPayload payload, Guid importingUserId)
    {
        var userId = new UserId(importingUserId);
        var tasks = payload.Tasks
            .Select(t => new TaskExecutionResult(
                t.TaskId,
                t.TaskName,
                t.ExecutionOrder,
                Enum.Parse<TaskExecutionStatus>(t.Status, ignoreCase: true),
                t.Output))
            .ToList();

        return new ExecutionResult(userId, tasks);
    }

    /// <summary>
    /// Builds the workflow context, synthesizing display names when absent.
    /// Synthesis rules are deterministic: same payload → same context values.
    /// </summary>
    private static ExecutionWorkflowContext BuildWorkflowContext(NormalizedImportPayload payload)
    {
        var name = !string.IsNullOrWhiteSpace(payload.WorkflowName)
            ? payload.WorkflowName
            : "Unnamed Import";

        var description = !string.IsNullOrWhiteSpace(payload.WorkflowDescription)
            ? payload.WorkflowDescription
            : $"Imported execution from {payload.CreatedAtUtc:yyyy-MM-dd}";

        return new ExecutionWorkflowContext(
            WorkflowId: null,
            WorkflowName: name,
            WorkflowDescription: description);
    }

    private async Task ImportAuditLogsAsync(string executionId, IReadOnlyList<NormalizedAuditEntry> entries)
    {
        foreach (var log in entries.OrderBy(l => l.Sequence))
        {
            if (!Enum.TryParse<AuditEventType>(log.EventType, ignoreCase: true, out var eventType))
                continue;

            await _auditLogger.LogImportedAsync(
                executionId,
                (int)log.Sequence,
                log.TimestampUtc,
                eventType,
                log.TaskId,
                log.Message ?? string.Empty);
        }
    }

    // Helpers 

    /// <summary>
    /// Resolves the output field regardless of whether it is a JSON object or a plain string.
    /// JSON objects are serialized back to a compact JSON string for storage in TaskExecutionResult.Output.
    /// </summary>
    private static string ResolveOutput(JsonElement taskElement)
    {
        if (!taskElement.TryGetProperty("output", out var outputEl))
            return string.Empty;

        return outputEl.ValueKind switch
        {
            JsonValueKind.String => outputEl.GetString() ?? string.Empty,
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            _ => outputEl.GetRawText()
        };
    }

    /// <summary>
    /// Normalizes status to PascalCase (e.g. "succeeded" → "Succeeded").
    /// </summary>
    private static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "Succeeded";

        return char.ToUpperInvariant(status[0]) + status[1..].ToLowerInvariant();
    }

    private static List<NormalizedAuditEntry> ParseAuditTrail(JsonElement root, string propertyName)
    {
        var entries = new List<NormalizedAuditEntry>();

        if (!root.TryGetProperty(propertyName, out var auditEl) || auditEl.ValueKind != JsonValueKind.Array)
            return entries;

        foreach (var log in auditEl.EnumerateArray())
        {
            var seq = log.TryGetProperty("sequence", out var seqEl) ? seqEl.GetInt64() : 0;
            var ts = log.TryGetProperty("timestampUtc", out var tsEl) && tsEl.TryGetDateTime(out var dt)
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                : DateTime.UtcNow;
            var eventType = log.TryGetProperty("eventType", out var etEl) ? etEl.GetString() ?? string.Empty : string.Empty;
            var taskId = log.TryGetProperty("taskId", out var tidEl) && tidEl.ValueKind != JsonValueKind.Null
                ? tidEl.GetString()
                : null;
            var message = log.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;

            entries.Add(new NormalizedAuditEntry(seq, ts, eventType, taskId, message));
        }

        return entries;
    }
}


internal sealed record NormalizedImportPayload(
    string ExecutionId,
    string Status,
    DateTime CreatedAtUtc,
    string? WorkflowName,
    string? WorkflowDescription,
    IReadOnlyList<NormalizedTaskPayload> Tasks,
    IReadOnlyList<NormalizedAuditEntry> AuditTrail
);

internal sealed record NormalizedTaskPayload(
    string TaskId,
    string TaskName,
    int ExecutionOrder,
    string Status,
    string Output
);

internal sealed record NormalizedAuditEntry(
    long Sequence,
    DateTime TimestampUtc,
    string EventType,
    string? TaskId,
    string? Message
);