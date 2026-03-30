using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.Telemetry;
namespace Arc.UnitTests.Execution;
using Arc.Infrastructure.Execution;


public class DeterministicExecutionExporterTests
{
    private readonly DeterministicExecutionExporter _exporter;
    private readonly MockExecutionResultStore _mockStore;
    private readonly MockAuditLogger _mockAuditLogger;

    public DeterministicExecutionExporterTests()
    {
        _mockStore = new MockExecutionResultStore();
        _mockAuditLogger = new MockAuditLogger();
        _exporter = new DeterministicExecutionExporter(_mockStore, _mockAuditLogger);
    }

    [Fact]
    public async Task ExportAsJsonAsync_ReturnsValidJson_WhenExecutionExists()
    {
        // Arrange
        var executionId = "exec-123";
        var userId = new UserId(Guid.NewGuid());
        var tasks = new List<TaskExecutionResult>
        {
            new("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output-1"),
            new("task-2", "Task 2", 2, TaskExecutionStatus.Succeeded, "output-2")
        };
        var result = new ExecutionResult(userId, tasks);

        _mockStore.Store(executionId, result);
        _mockAuditLogger.LogExecution(executionId, new List<AuditLogEntry>());

        // Act
        var json = await _exporter.ExportAsJsonAsync(executionId);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("executionId");
        json.Should().Contain("userId");
        json.Should().Contain("tasks");
    }

    [Fact]
    public async Task ExportAsJsonAsync_ReturnsDeterministicOutput_ForSameInput()
    {
        // Arrange
        var executionId = "exec-123";
        var userId = new UserId(Guid.NewGuid());
        var tasks = new List<TaskExecutionResult>
        {
            new("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output-1")
        };
        var result = new ExecutionResult(userId, tasks);

        _mockStore.Store(executionId, result);
        _mockAuditLogger.LogExecution(executionId, new List<AuditLogEntry>());

        // Act
        var export1 = await _exporter.ExportAsJsonAsync(executionId);
        var export2 = await _exporter.ExportAsJsonAsync(executionId);

        // Assert - Same input produces identical output (deterministic)
        export1.Should().Be(export2);
    }

    [Fact]
    public async Task ExportAsJsonAsync_ReturnsNull_WhenExecutionNotFound()
    {
        // Act
        var json = await _exporter.ExportAsJsonAsync("non-existent-id");

        // Assert
        json.Should().BeNull();
    }

    [Fact]
    public async Task ExportAsCSVAsync_ReturnsValidCSV_WhenExecutionExists()
    {
        // Arrange
        var executionId = "exec-123";
        var userId = new UserId(Guid.NewGuid());
        var tasks = new List<TaskExecutionResult>
        {
            new("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output-1"),
            new("task-2", "Task 2", 2, TaskExecutionStatus.Succeeded, "output-2")
        };
        var result = new ExecutionResult(userId, tasks);

        _mockStore.Store(executionId, result);

        // Act
        var csv = await _exporter.ExportAsCSVAsync(executionId);

        // Assert
        csv.Should().NotBeNullOrEmpty();
        csv.Should().Contain("ExecutionId,TaskId,TaskName,ExecutionOrder,Status,Output");
        csv.Should().Contain("task-1");
        csv.Should().Contain("task-2");
    }

    [Fact]
    public async Task ExportAsCSVAsync_ReturnsDeterministicOutput_ForSameInput()
    {
        // Arrange
        var executionId = "exec-123";
        var userId = new UserId(Guid.NewGuid());
        var tasks = new List<TaskExecutionResult>
        {
            new("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output-1")
        };
        var result = new ExecutionResult(userId, tasks);

        _mockStore.Store(executionId, result);

        // Act
        var export1 = await _exporter.ExportAsCSVAsync(executionId);
        var export2 = await _exporter.ExportAsCSVAsync(executionId);

        // Assert
        export1.Should().Be(export2);
    }

    [Fact]
    public async Task ExportAuditLogsAsJsonAsync_ReturnsValidJson_WhenExecutionExists()
    {
        // Arrange
        var executionId = "exec-123";
        var userId = new UserId(Guid.NewGuid());
        var tasks = new List<TaskExecutionResult>
        {
            new("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output-1")
        };
        var result = new ExecutionResult(userId, tasks);

        _mockStore.Store(executionId, result);
        var auditLogs = new List<AuditLogEntry>
        {
            new(executionId, 1, DateTime.UtcNow, AuditEventType.OrchestratorStarted, null, "Started"),
            new(executionId, 2, DateTime.UtcNow, AuditEventType.OrchestratorFinished, null, "Finished")
        };
        _mockAuditLogger.LogExecution(executionId, auditLogs);

        // Act
        var json = await _exporter.ExportAuditLogsAsJsonAsync(executionId);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("logs");
        json.Should().Contain("OrchestratorStarted");
    }

    // Mock implementations
    private class MockExecutionResultStore : IExecutionResultStore
    {
        private readonly Dictionary<string, ExecutionResult> _store = new();

        public void Store(string executionId, ExecutionResult result, DateTime? createdAtUtc = null)
        {
            _store[executionId] = result;
        }

        public Task StoreAsync(
            string executionId,
            ExecutionResult result,
            DateTime createdAtUtc,
            ExecutionWorkflowContext? workflowContext)
        {
            Store(executionId, result, createdAtUtc);
            return Task.CompletedTask;
        }

        public Task<ExecutionResult?> GetAsync(string executionId)
        {
            _store.TryGetValue(executionId, out var result);
            return Task.FromResult(result);
        }

        public Task<ExecutionWorkflowContext?> GetWorkflowContextAsync(string executionId)
            => Task.FromResult<ExecutionWorkflowContext?>(null);

        public Task<ExecutionQueryResult> QueryAsync(ExecutionQueryFilter? filter, PaginationParams pagination, Guid userId)
        {
            var items = _store.Keys
                .Select(id => new ExecutionMetadata(id, DateTime.UtcNow, 1, 100, "Succeeded", string.Empty, string.Empty, false))
                .ToList();
            var analytics = new ExecutionAnalytics(items.Count, items.Count, 0, 1.0, 1, 100);
            return Task.FromResult(new ExecutionQueryResult(items, analytics, pagination.Limit, pagination.Offset, items.Count));
        }

        public Task ArchiveAsync(string executionId, Guid archivedBy, string? reason = null, int? retentionDays = null) => Task.CompletedTask;
        public Task UnarchiveAsync(string executionId, Guid unarchivedBy) => Task.CompletedTask;
        public Task PurgeAsync(string executionId, Guid purgedBy, string? reason = null) => Task.CompletedTask;
        public Task<IReadOnlyList<ArchiveAuditEntry>> GetArchiveAuditAsync(string executionId) => Task.FromResult<IReadOnlyList<ArchiveAuditEntry>>(new List<ArchiveAuditEntry>());
    }

    private class MockAuditLogger : IAuditLogger
    {
        private readonly Dictionary<string, List<AuditLogEntry>> _logs = new();

        public void LogExecution(string executionId, List<AuditLogEntry> logs)
        {
            _logs[executionId] = logs;
        }

        public Task LogAsync(string executionId, AuditEventType eventType, string? taskId = null, string? message = null)
            => Task.CompletedTask;

        public Task LogImportedAsync(
            string executionId,
            long sequence,
            DateTime timestampUtc,
            AuditEventType eventType,
            string? taskId = null,
            string? message = null)
            => Task.CompletedTask;

        public Task<IReadOnlyList<AuditLogEntry>> GetExecutionLogsAsync(string executionId)
        {
            _logs.TryGetValue(executionId, out var logs);
            return Task.FromResult((IReadOnlyList<AuditLogEntry>)(logs ?? new List<AuditLogEntry>()));
        }

        public Task<IReadOnlyList<AuditLogEntry>> GetExecutionLogsAsync(string executionId, AuditEventType? eventType, string? taskId)
            => GetExecutionLogsAsync(executionId);
    }
}