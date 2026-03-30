namespace Arc.Application.Execution;

/// <summary>
/// Service for deterministically exporting execution data in various formats.
/// All exports are fully deterministic: identical input → identical output.
/// </summary>
/// 
public interface IExecutionExporter
{
    /// <summary>
    /// Exports a single execution as JSON with complete audit trail.
    /// </summary>
    /// <param name="executionId">The execution ID to export.</param>
    /// <returns>JSON-serialized execution data or null if not found.</returns>
    Task<string?> ExportAsJsonAsync(string executionId);

    /// <summary>
    /// Exports multiple executions as JSON array with filter support.
    /// Results ordered deterministically by ExecutionId (ascending).
    /// </summary>
    /// <param name="filter">Query filter for selecting executions.</param>
    /// <param name="pagination">Pagination parameters.</param>
    /// <returns>JSON-serialized array of execution data.</returns>
    Task<string> ExportBulkAsJsonAsync(ExecutionQueryFilter filter, PaginationParams pagination, Guid userId);

    /// <summary>
    /// Exports a single execution as CSV format (tasks only).
    /// Includes: ExecutionId, TaskId, TaskName, Status, Output, ExecutionOrder, DependsOn
    /// </summary>
    /// <param name="executionId">The execution ID to export.</param>
    /// <returns>CSV-formatted string or null if not found.</returns>
    Task<string?> ExportAsCSVAsync(string executionId);

    /// <summary>
    /// Exports multiple executions as CSV format with headers.
    /// One row per task with ExecutionId included.
    /// </summary>
    /// <param name="filter">Query filter for selecting executions.</param>
    /// <param name="pagination">Pagination parameters.</param>
    /// <returns>CSV-formatted string.</returns>
    Task<string> ExportBulkAsCSVAsync(ExecutionQueryFilter filter, PaginationParams pagination, Guid userId);

    /// <summary>
    /// Exports audit logs for a specific execution as JSON array.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <returns>JSON-serialized array of audit log entries.</returns>
    Task<string?> ExportAuditLogsAsJsonAsync(string executionId);
}