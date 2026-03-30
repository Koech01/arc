using FluentAssertions;
using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.Telemetry;
namespace Arc.UnitTests.Execution;
using Arc.Infrastructure.Execution;


public class DeterministicExecutionImporterTests
{
    private readonly DeterministicExecutionImporter _importer;
    private readonly MockExecutionResultStore _mockStore;
    private readonly MockAuditLogger _mockAuditLogger;

    public DeterministicExecutionImporterTests()
    {
        _mockStore = new MockExecutionResultStore();
        _mockAuditLogger = new MockAuditLogger();
        _importer = new DeterministicExecutionImporter(_mockStore, _mockAuditLogger);
    }

    [Fact]
    public async Task ImportFromJsonAsync_SucceedsWithValidData()
    {
        // Arrange
        var jsonData = """
        {
          "executionId": "exec-123",
          "createdAtUtc": "2026-01-01T00:00:00Z",
          "status": "Succeeded",
          "tasks": [
            {
              "taskId": "task-1",
              "taskName": "Task 1",
              "executionOrder": 1,
              "status": "Succeeded",
              "output": "output-1"
            }
          ]
        }
        """;

        // Act
        var result = await _importer.ImportFromJsonAsync(jsonData, Guid.NewGuid());

        // Assert
        result.Should().NotBeNull();
        result!.Tasks.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateJsonImportAsync_ReturnsValid_ForCorrectData()
    {
        // Arrange
        var jsonData = """
        {
          "executionId": "exec-123",
          "createdAtUtc": "2026-01-01T00:00:00Z",
          "status": "Succeeded",
          "tasks": [
            {
              "taskId": "task-1",
              "taskName": "Task 1",
              "executionOrder": 1,
              "status": "Succeeded",
              "output": "output-1"
            }
          ]
        }
        """;

        // Act
        var result = await _importer.ValidateJsonImportAsync(jsonData);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateJsonImportAsync_ReturnsInvalid_ForMissingExecutionId()
    {
        // Arrange
        var jsonData = """
        {
          "tasks": []
        }
        """;

        // Act
        var result = await _importer.ValidateJsonImportAsync(jsonData);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateJsonImportAsync_ReturnsInvalid_ForEmptyTaskList()
    {
        // Arrange - userId in the payload is intentionally ignored (security invariant);
        // an empty tasks array is the actual validation failure here.
        var jsonData = """
        {
          "executionId": "exec-123",
          "tasks": []
        }
        """;

        // Act
        var result = await _importer.ValidateJsonImportAsync(jsonData);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("task"));
    }

    [Fact]
    public async Task ImportBulkFromJsonAsync_ProcessesMultipleExecutions()
    {
        // Arrange
        var jsonData = """
        [
          {
            "executionId": "exec-1",
            "createdAtUtc": "2026-01-01T00:00:00Z",
            "status": "Succeeded",
            "tasks": [
              {
                "taskId": "task-1",
                "taskName": "Task 1",
                "executionOrder": 1,
                "status": "Succeeded",
                "output": "output-1"
              }
            ]
          },
          {
            "executionId": "exec-2",
            "createdAtUtc": "2026-01-01T00:00:00Z",
            "status": "Succeeded",
            "tasks": [
              {
                "taskId": "task-2",
                "taskName": "Task 2",
                "executionOrder": 1,
                "status": "Succeeded",
                "output": "output-2"
              }
            ]
          }
        ]
        """;

        // Act
        var results = await _importer.ImportBulkFromJsonAsync(jsonData, Guid.NewGuid());

        // Assert
        results.Should().HaveCount(2);
        results.All(r => r.Success).Should().BeTrue();
    }

    [Fact]
    public async Task ImportBulkFromJsonAsync_ContinuesOnError()
    {
        // Arrange - exec-2 has an empty task list, which fails validation.
        // exec-1 and exec-3 are valid and should succeed.
        var jsonData = """
        [
          {
            "executionId": "exec-1",
            "createdAtUtc": "2026-01-01T00:00:00Z",
            "status": "Succeeded",
            "tasks": [
              {
                "taskId": "task-1",
                "taskName": "Task 1",
                "executionOrder": 1,
                "status": "Succeeded",
                "output": "output-1"
              }
            ]
          },
          {
            "executionId": "exec-2",
            "createdAtUtc": "2026-01-01T00:00:00Z",
            "status": "Succeeded",
            "tasks": []
          },
          {
            "executionId": "exec-3",
            "createdAtUtc": "2026-01-01T00:00:00Z",
            "status": "Succeeded",
            "tasks": [
              {
                "taskId": "task-3",
                "taskName": "Task 3",
                "executionOrder": 1,
                "status": "Succeeded",
                "output": "output-3"
              }
            ]
          }
        ]
        """;

        // Act
        var results = await _importer.ImportBulkFromJsonAsync(jsonData, Guid.NewGuid());

        // Assert
        results.Should().HaveCount(3);
        results.Count(r => r.Success).Should().Be(2);
        results.Count(r => !r.Success).Should().Be(1);
    }

    // Mock implementations
    private class MockExecutionResultStore : IExecutionResultStore
    {
        private readonly Dictionary<string, ExecutionResult> _store = new();

        public Task StoreAsync(
            string executionId,
            ExecutionResult result,
            DateTime createdAtUtc,
            ExecutionWorkflowContext? workflowContext)
        {
            _store[executionId] = result;
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
            return Task.FromResult(new ExecutionQueryResult(
                new List<ExecutionMetadata>(),
                new ExecutionAnalytics(0, 0, 0, 0, 0, 0),
                0,
                0,
                0));
        }

        public Task ArchiveAsync(string executionId, Guid archivedBy, string? reason = null, int? retentionDays = null) => Task.CompletedTask;
        public Task UnarchiveAsync(string executionId, Guid unarchivedBy) => Task.CompletedTask;
        public Task PurgeAsync(string executionId, Guid purgedBy, string? reason = null) => Task.CompletedTask;
        public Task<IReadOnlyList<ArchiveAuditEntry>> GetArchiveAuditAsync(string executionId) => Task.FromResult<IReadOnlyList<ArchiveAuditEntry>>(new List<ArchiveAuditEntry>());
    }

    private class MockAuditLogger : IAuditLogger
    {
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
            => Task.FromResult((IReadOnlyList<AuditLogEntry>)new List<AuditLogEntry>());

        public Task<IReadOnlyList<AuditLogEntry>> GetExecutionLogsAsync(string executionId, AuditEventType? eventType, string? taskId)
            => GetExecutionLogsAsync(executionId);
    }
}