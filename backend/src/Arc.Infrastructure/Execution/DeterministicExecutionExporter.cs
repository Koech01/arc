using System.Text;
using System.Text.Json; 
using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.Telemetry;
using System.Text.Json.Serialization;
namespace Arc.Infrastructure.Execution;


/// <summary>
/// Deterministic implementation of execution exporter.
/// Exports execution data in JSON and CSV formats with guaranteed consistency.
/// Same input always produces identical output (deterministic).
/// </summary>
public sealed class DeterministicExecutionExporter : IExecutionExporter
{
    private readonly IExecutionResultStore _resultStore;
    private readonly IAuditLogger _auditLogger;

    public DeterministicExecutionExporter(
        IExecutionResultStore resultStore,
        IAuditLogger auditLogger)
    {
        _resultStore = resultStore ?? throw new ArgumentNullException(nameof(resultStore));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    }

    public async Task<string?> ExportAsJsonAsync(string executionId)
    {
        var execution = await _resultStore.GetAsync(executionId);
        if (execution == null)
            return null;

        var auditLogs = await _auditLogger.GetExecutionLogsAsync(executionId);

        var allSucceeded = execution.Tasks.All(t => t.Status == TaskExecutionStatus.Succeeded);
        var exportStatus = allSucceeded ? "Succeeded" : "Failed";

        var createdAtUtc =
            auditLogs.FirstOrDefault(l => l.EventType == AuditEventType.OrchestratorStarted)?.TimestampUtc
            ?? auditLogs.FirstOrDefault()?.TimestampUtc
            ?? DateTime.MinValue;

        var exportData = new ExecutionExportDto
        {
            ExecutionId = executionId,
            UserId = execution.UserId.Value.ToString(),
            Status = exportStatus,
            CreatedAtUtc = createdAtUtc,
            Tasks = execution.Tasks
                .Select(t => new ExportedTaskDto
                {
                    TaskId = t.TaskId,
                    TaskName = t.TaskName,
                    ExecutionOrder = t.ExecutionOrder,
                    Status = t.Status.ToString(),
                    Output = t.Output
                })
                .ToList(),
            AuditLogs = auditLogs
                .Select(log => new ExportedAuditLogDto
                {
                    Sequence = log.Sequence,
                    TimestampUtc = log.TimestampUtc,
                    EventType = log.EventType.ToString(),
                    TaskId = log.TaskId,
                    Message = log.Message
                })
                .ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(exportData, options);
    }

    public async Task<string> ExportBulkAsJsonAsync(ExecutionQueryFilter filter, PaginationParams pagination, Guid userId)
    {
        var queryResult = await _resultStore.QueryAsync(filter, pagination, userId);
        var allExecutions = new List<ExecutionExportDto>();

        // Export each execution with full details
        foreach (var metadata in queryResult.Executions)
        {
            var execution = await _resultStore.GetAsync(metadata.ExecutionId);
            if (execution == null)
                continue;

            var auditLogs = await _auditLogger.GetExecutionLogsAsync(metadata.ExecutionId);

            var exportStatus = metadata.Status == "Succeeded" ? "Succeeded" : "Failed";

            var exportData = new ExecutionExportDto
            {
                ExecutionId = metadata.ExecutionId,
                UserId = execution.UserId.Value.ToString(),
                Status = exportStatus,
                CreatedAtUtc = metadata.CreatedAtUtc,
                Tasks = execution.Tasks
                    .Select(t => new ExportedTaskDto
                    {
                        TaskId = t.TaskId,
                        TaskName = t.TaskName,
                        ExecutionOrder = t.ExecutionOrder,
                        Status = t.Status.ToString(),
                        Output = t.Output
                    })
                    .ToList(),
                AuditLogs = auditLogs
                    .Select(log => new ExportedAuditLogDto
                    {
                        Sequence = log.Sequence,
                        TimestampUtc = log.TimestampUtc,
                        EventType = log.EventType.ToString(),
                        TaskId = log.TaskId,
                        Message = log.Message
                    })
                    .ToList()
            };

            allExecutions.Add(exportData);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(allExecutions, options);
    }

    public async Task<string?> ExportAsCSVAsync(string executionId)
    {
        var execution = await _resultStore.GetAsync(executionId);
        if (execution == null)
            return null;

        var csv = new StringBuilder();
        csv.AppendLine("ExecutionId,TaskId,TaskName,ExecutionOrder,Status,Output");

        foreach (var task in execution.Tasks.OrderBy(t => t.ExecutionOrder))
        {
            var output = EscapeCsvField(task.Output);
            csv.AppendLine($"{executionId},{EscapeCsvField(task.TaskId)},{EscapeCsvField(task.TaskName)},{task.ExecutionOrder},{task.Status},{output}");
        }

        return csv.ToString();
    }

    public async Task<string> ExportBulkAsCSVAsync(ExecutionQueryFilter filter, PaginationParams pagination, Guid userId)
    {
        var queryResult = await _resultStore.QueryAsync(filter, pagination, userId);

        var csv = new StringBuilder();
        csv.AppendLine("ExecutionId,TaskId,TaskName,ExecutionOrder,Status,Output");

        foreach (var metadata in queryResult.Executions)
        {
            var execution = await _resultStore.GetAsync(metadata.ExecutionId);
            if (execution == null)
                continue;

            foreach (var task in execution.Tasks.OrderBy(t => t.ExecutionOrder))
            {
                var output = EscapeCsvField(task.Output);
                csv.AppendLine($"{metadata.ExecutionId},{EscapeCsvField(task.TaskId)},{EscapeCsvField(task.TaskName)},{task.ExecutionOrder},{task.Status},{output}");
            }
        }

        return csv.ToString();
    }

    public async Task<string?> ExportAuditLogsAsJsonAsync(string executionId)
    {
        var execution = await _resultStore.GetAsync(executionId);
        if (execution == null)
            return null;

        var auditLogs = await _auditLogger.GetExecutionLogsAsync(executionId);

        var exportData = new AuditLogsExportDto
        {
            ExecutionId = executionId,
            ExportedAtUtc = auditLogs.Count > 0
                ? auditLogs[^1].TimestampUtc
                : DateTime.MinValue,
            LogCount = auditLogs.Count,
            Logs = auditLogs
                .Select(log => new ExportedAuditLogDto
                {
                    Sequence = log.Sequence,
                    TimestampUtc = log.TimestampUtc,
                    EventType = log.EventType.ToString(),
                    TaskId = log.TaskId,
                    Message = log.Message
                })
                .ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(exportData, options);
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains("\"") || value.Contains(",") || value.Contains("\n"))
            return "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }
}

/// <summary>
/// DTO for exporting a complete execution with tasks and audit logs.
/// </summary>
public sealed class ExecutionExportDto
{
    public required string ExecutionId { get; set; }
    public required string UserId { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public required List<ExportedTaskDto> Tasks { get; set; }
    public List<ExportedAuditLogDto> AuditLogs { get; set; } = new();
}

/// <summary>
/// DTO for a task in export format.
/// </summary>
public sealed class ExportedTaskDto
{
    public required string TaskId { get; set; }
    public required string TaskName { get; set; }
    public int ExecutionOrder { get; set; }
    public required string Status { get; set; }
    public required string Output { get; set; }
}

/// <summary>
/// DTO for audit log entry in export format.
/// </summary>
public sealed class ExportedAuditLogDto
{
    public long Sequence { get; set; }
    public DateTime TimestampUtc { get; set; }
    public required string EventType { get; set; }
    public string? TaskId { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// DTO for exporting audit logs only.
/// </summary>
public sealed class AuditLogsExportDto
{
    public required string ExecutionId { get; set; }
    public DateTime ExportedAtUtc { get; set; }
    public int LogCount { get; set; }
    public required List<ExportedAuditLogDto> Logs { get; set; }
}