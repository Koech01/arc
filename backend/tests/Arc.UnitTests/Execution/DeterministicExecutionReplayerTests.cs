using NSubstitute;
using Arc.Domain.Models;
using FluentAssertions;
using Arc.Application.Results;
using Arc.Application.Telemetry;
using Arc.Application.Execution;
namespace Arc.UnitTests.Execution;
using Arc.Infrastructure.Execution;


/// <summary>
/// Unit tests for DeterministicExecutionReplayer.
/// Verifies that replay reconstructs execution state deterministically from persisted data.
/// </summary>
public sealed class DeterministicExecutionReplayerTests
{
    [Fact]
    public async Task ReplayAsync_WithValidExecutionId_ReturnsStoredExecutionResult()
    {
        // Arrange
        var executionId = "task-a-task-b";
        var tasks = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "output-a"),
            new TaskExecutionResult("task-b", "Task B", 2, TaskExecutionStatus.Succeeded, "output-b")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);
        var auditLogs = new List<AuditLogEntry>
        {
            new("exec-id", 1, DateTime.UtcNow, AuditEventType.OrchestratorStarted, null, "Execution started"),
            new("exec-id", 2, DateTime.UtcNow.AddMilliseconds(1), AuditEventType.TaskStarted, "task-a", "Task A started"),
            new("exec-id", 3, DateTime.UtcNow.AddMilliseconds(2), AuditEventType.TaskFinished, "task-a", "Task A finished"),
            new("exec-id", 4, DateTime.UtcNow.AddMilliseconds(3), AuditEventType.OrchestratorFinished, null, "Execution finished")
        };

        var resultStore = Substitute.For<IExecutionResultStore>();
        resultStore.GetAsync(executionId).Returns(result);

        var auditLogger = Substitute.For<IAuditLogger>();
        auditLogger.GetExecutionLogsAsync(executionId).Returns(auditLogs);

        var replayer = new DeterministicExecutionReplayer(resultStore, auditLogger);

        // Act
        var replayResult = await replayer.ReplayAsync(executionId);

        // Assert
        replayResult.Should().NotBeNull();
        replayResult!.ExecutionId.Should().Be(executionId);
        replayResult.Tasks.Should().HaveCount(2);
        replayResult.Tasks.First().TaskId.Should().Be("task-a");
        replayResult.Tasks.Last().TaskId.Should().Be("task-b");
        replayResult.AuditTrace.Should().HaveCount(4);
        replayResult.AuditTrace.First().EventType.Should().Be("OrchestratorStarted");
    }

    [Fact]
    public async Task ReplayAsync_WithNonExistentExecutionId_ReturnsNull()
    {
        // Arrange
        var executionId = "non-existent";
        var resultStore = Substitute.For<IExecutionResultStore>();
        resultStore.GetAsync(executionId).Returns((ExecutionResult?)null);

        var auditLogger = Substitute.For<IAuditLogger>();

        var replayer = new DeterministicExecutionReplayer(resultStore, auditLogger);

        // Act
        var replayResult = await replayer.ReplayAsync(executionId);

        // Assert
        replayResult.Should().BeNull();
    }

    [Fact]
    public async Task ReplayAsync_WithEmptyExecutionId_ThrowsArgumentException()
    {
        // Arrange
        var resultStore = Substitute.For<IExecutionResultStore>();
        var auditLogger = Substitute.For<IAuditLogger>();
        var replayer = new DeterministicExecutionReplayer(resultStore, auditLogger);

        // Act & Assert
        await replayer.Invoking(r => r.ReplayAsync(string.Empty))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ReplayAsync_PreservesTaskOrderDeterministically()
    {
        // Arrange
        var executionId = "deterministic-order-test";
        var tasks = new[]
        {
            new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out-1"),
            new TaskExecutionResult("task-2", "Task 2", 2, TaskExecutionStatus.Succeeded, "out-2"),
            new TaskExecutionResult("task-3", "Task 3", 3, TaskExecutionStatus.Succeeded, "out-3")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);
        var auditLogs = Array.Empty<AuditLogEntry>();

        var resultStore = Substitute.For<IExecutionResultStore>();
        resultStore.GetAsync(executionId).Returns(result);

        var auditLogger = Substitute.For<IAuditLogger>();
        auditLogger.GetExecutionLogsAsync(executionId).Returns(auditLogs);

        var replayer = new DeterministicExecutionReplayer(resultStore, auditLogger);

        // Act - Replay multiple times
        var replay1 = await replayer.ReplayAsync(executionId);
        var replay2 = await replayer.ReplayAsync(executionId);

        // Assert - Order must be identical
        replay1!.Tasks.Select(t => t.ExecutionOrder).Should().Equal(replay2!.Tasks.Select(t => t.ExecutionOrder));
        replay1.Tasks.Select(t => t.TaskId).Should().Equal("task-1", "task-2", "task-3");
    }

    [Fact]
    public async Task ReplayAsync_IncludesAuditTraceFromLogger()
    {
        // Arrange
        var executionId = "audit-trace-test";
        var task = new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "output");
        var result = new ExecutionResult(UserId.Anonymous, new[] { task });

        var auditLogs = new List<AuditLogEntry>
        {
            new("exec-id", 1, DateTime.UtcNow, AuditEventType.OrchestratorStarted, null, "Started"),
            new("exec-id", 2, DateTime.UtcNow, AuditEventType.TaskStarted, "task-a", "Task A started"),
            new("exec-id", 3, DateTime.UtcNow, AuditEventType.TaskFinished, "task-a", "Task A succeeded")
        };

        var resultStore = Substitute.For<IExecutionResultStore>();
        resultStore.GetAsync(executionId).Returns(result);

        var auditLogger = Substitute.For<IAuditLogger>();
        auditLogger.GetExecutionLogsAsync(executionId).Returns(auditLogs);

        var replayer = new DeterministicExecutionReplayer(resultStore, auditLogger);

        // Act
        var replayResult = await replayer.ReplayAsync(executionId);

        // Assert
        replayResult!.AuditTrace.Should().HaveCount(3);
        replayResult.AuditTrace[0].EventType.Should().Be("OrchestratorStarted");
        replayResult.AuditTrace[1].TaskId.Should().Be("task-a");
        replayResult.AuditTrace[1].Message.Should().Be("Task A started");
    }
}
