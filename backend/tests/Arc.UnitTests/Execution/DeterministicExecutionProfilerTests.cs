using NSubstitute;
using Arc.Domain.Models;
using FluentAssertions;
using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.Telemetry;
namespace Arc.UnitTests.Execution;
using Arc.Infrastructure.Execution;

public sealed class DeterministicExecutionProfilerTests
{
    private readonly IExecutionResultStore _mockResultStore;
    private readonly IAuditLogger _mockAuditLogger;
    private readonly DeterministicExecutionProfiler _profiler;

    public DeterministicExecutionProfilerTests()
    {
        _mockResultStore = Substitute.For<IExecutionResultStore>();
        _mockAuditLogger = Substitute.For<IAuditLogger>();
        _profiler = new DeterministicExecutionProfiler(_mockResultStore, _mockAuditLogger);
    }

    [Fact]
    public async Task GenerateProfileAsync_WithValidExecution_ReturnsProfile()
    {
        // Arrange
        const string executionId = "test-execution-id";
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var executionResult = new ExecutionResult(UserId.Anonymous, new[]
        {
            new TaskExecutionResult("task1", "Task 1", 1, TaskExecutionStatus.Succeeded, "Output 1"),
            new TaskExecutionResult("task2", "Task 2", 2, TaskExecutionStatus.Succeeded, "Output 2"),
            new TaskExecutionResult("task3", "Task 3", 3, TaskExecutionStatus.Succeeded, "Output 3")
        });

        var auditLogs = new List<AuditLogEntry>
        {
            new(executionId, 1, baseTime, AuditEventType.OrchestratorStarted, null, "Execution started"),
            new(executionId, 2, baseTime.AddMilliseconds(10), AuditEventType.TaskStarted, "task1", "Task execution started"),
            new(executionId, 3, baseTime.AddMilliseconds(110), AuditEventType.TaskFinished, "task1", "Task executed and cached"),
            new(executionId, 4, baseTime.AddMilliseconds(120), AuditEventType.TaskStarted, "task2", "Task execution started"),
            new(executionId, 5, baseTime.AddMilliseconds(320), AuditEventType.TaskFinished, "task2", "Task executed and cached"),
            new(executionId, 6, baseTime.AddMilliseconds(330), AuditEventType.TaskStarted, "task3", "Task execution started"),
            new(executionId, 7, baseTime.AddMilliseconds(480), AuditEventType.TaskFinished, "task3", "Task executed and cached"),
            new(executionId, 8, baseTime.AddMilliseconds(490), AuditEventType.OrchestratorFinished, null, "Execution finished")
        };

        _mockResultStore.GetAsync(executionId).Returns(executionResult);
        _mockAuditLogger.GetExecutionLogsAsync(executionId).Returns(auditLogs);

        // Act
        var profile = await _profiler.GenerateProfileAsync(executionId);

        // Assert
        profile.Should().NotBeNull();
        profile!.ExecutionId.Should().Be(executionId);
        profile.TaskMetrics.Should().HaveCount(3);

        // Verify task metrics
        var task1Metrics = profile.TaskMetrics.First(t => t.TaskId == "task1");
        task1Metrics.ExecutionTimeMs.Should().Be(100); // 110ms - 10ms
        task1Metrics.DependencyWaitTimeMs.Should().Be(0); // First task has no dependencies
        task1Metrics.IsCriticalPath.Should().BeTrue();

        var task2Metrics = profile.TaskMetrics.First(t => t.TaskId == "task2");
        task2Metrics.ExecutionTimeMs.Should().Be(200); // 320ms - 120ms
        task2Metrics.DependencyWaitTimeMs.Should().Be(10); // 120ms - 110ms
        task2Metrics.IsCriticalPath.Should().BeTrue();

        var task3Metrics = profile.TaskMetrics.First(t => t.TaskId == "task3");
        task3Metrics.ExecutionTimeMs.Should().Be(150); // 480ms - 330ms
        task3Metrics.DependencyWaitTimeMs.Should().Be(10); // 330ms - 320ms
        task3Metrics.IsCriticalPath.Should().BeTrue();

        // Verify critical path analysis
        profile.CriticalPath.CriticalPathTaskIds.Should().BeEquivalentTo(new[] { "task1", "task2", "task3" });
        profile.CriticalPath.TotalCriticalPathTimeMs.Should().Be(450); // 100 + 200 + 150
        profile.CriticalPath.CriticalPathPercentage.Should().Be(100.0);

        // Verify resource utilization
        profile.ResourceUtilization.TotalExecutionTimeMs.Should().Be(450);
        profile.ResourceUtilization.ParallelizableTimeMs.Should().Be(0);
        profile.ResourceUtilization.SequentialTimeMs.Should().Be(450);
        profile.ResourceUtilization.ParallelizationEfficiency.Should().Be(0.0);
        profile.ResourceUtilization.MaxConcurrentTasks.Should().Be(1);
        profile.ResourceUtilization.AverageTaskExecutionTimeMs.Should().Be(150.0); // 450 / 3
    }

    [Fact]
    public async Task GenerateProfileAsync_WithNonExistentExecution_ReturnsNull()
    {
        // Arrange
        const string executionId = "non-existent-id";
        _mockResultStore.GetAsync(executionId).Returns((ExecutionResult?)null);

        // Act
        var profile = await _profiler.GenerateProfileAsync(executionId);

        // Assert
        profile.Should().BeNull();
    }

    [Fact]
    public async Task GenerateProfileAsync_WithEmptyAuditLogs_ReturnsNull()
    {
        // Arrange
        const string executionId = "test-execution-id";
        var executionResult = new ExecutionResult(UserId.Anonymous, new[]
        {
            new TaskExecutionResult("task1", "Task 1", 1, TaskExecutionStatus.Succeeded, "Output 1")
        });

        _mockResultStore.GetAsync(executionId).Returns(executionResult);
        _mockAuditLogger.GetExecutionLogsAsync(executionId).Returns(new List<AuditLogEntry>());

        // Act
        var profile = await _profiler.GenerateProfileAsync(executionId);

        // Assert
        profile.Should().BeNull();
    }

    [Fact]
    public async Task GenerateProfileAsync_WithSingleTask_ReturnsValidProfile()
    {
        // Arrange
        const string executionId = "single-task-execution";
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var executionResult = new ExecutionResult(UserId.Anonymous, new[]
        {
            new TaskExecutionResult("task1", "Single Task", 1, TaskExecutionStatus.Succeeded, "Output")
        });

        var auditLogs = new List<AuditLogEntry>
        {
            new(executionId, 1, baseTime, AuditEventType.OrchestratorStarted, null, "Execution started"),
            new(executionId, 2, baseTime.AddMilliseconds(10), AuditEventType.TaskStarted, "task1", "Task execution started"),
            new(executionId, 3, baseTime.AddMilliseconds(110), AuditEventType.TaskFinished, "task1", "Task executed and cached"),
            new(executionId, 4, baseTime.AddMilliseconds(120), AuditEventType.OrchestratorFinished, null, "Execution finished")
        };

        _mockResultStore.GetAsync(executionId).Returns(executionResult);
        _mockAuditLogger.GetExecutionLogsAsync(executionId).Returns(auditLogs);

        // Act
        var profile = await _profiler.GenerateProfileAsync(executionId);

        // Assert
        profile.Should().NotBeNull();
        profile!.TaskMetrics.Should().HaveCount(1);

        var taskMetrics = profile.TaskMetrics.First();
        taskMetrics.TaskId.Should().Be("task1");
        taskMetrics.ExecutionTimeMs.Should().Be(100);
        taskMetrics.DependencyWaitTimeMs.Should().Be(0);
        taskMetrics.IsCriticalPath.Should().BeTrue();
        taskMetrics.Dependencies.Should().BeEmpty();

        profile.CriticalPath.CriticalPathTaskIds.Should().BeEquivalentTo(new[] { "task1" });
        profile.CriticalPath.TotalCriticalPathTimeMs.Should().Be(100);
        profile.CriticalPath.CriticalPathPercentage.Should().Be(100.0);

        profile.ResourceUtilization.TotalExecutionTimeMs.Should().Be(100);
        profile.ResourceUtilization.AverageTaskExecutionTimeMs.Should().Be(100.0);
    }

    [Fact]
    public async Task GenerateProfileAsync_WithMissingTimingData_HandlesGracefully()
    {
        // Arrange
        const string executionId = "missing-timing-execution";
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var executionResult = new ExecutionResult(UserId.Anonymous, new[]
        {
            new TaskExecutionResult("task1", "Task 1", 1, TaskExecutionStatus.Succeeded, "Output 1"),
            new TaskExecutionResult("task2", "Task 2", 2, TaskExecutionStatus.Succeeded, "Output 2")
        });

        // Audit logs missing some timing data
        var auditLogs = new List<AuditLogEntry>
        {
            new(executionId, 1, baseTime, AuditEventType.OrchestratorStarted, null, "Execution started"),
            new(executionId, 2, baseTime.AddMilliseconds(10), AuditEventType.TaskStarted, "task1", "Task execution started"),
            // Missing TaskFinished for task1
            new(executionId, 3, baseTime.AddMilliseconds(120), AuditEventType.TaskStarted, "task2", "Task execution started"),
            new(executionId, 4, baseTime.AddMilliseconds(320), AuditEventType.TaskFinished, "task2", "Task executed and cached"),
            new(executionId, 5, baseTime.AddMilliseconds(330), AuditEventType.OrchestratorFinished, null, "Execution finished")
        };

        _mockResultStore.GetAsync(executionId).Returns(executionResult);
        _mockAuditLogger.GetExecutionLogsAsync(executionId).Returns(auditLogs);

        // Act
        var profile = await _profiler.GenerateProfileAsync(executionId);

        // Assert
        profile.Should().NotBeNull();
        profile!.TaskMetrics.Should().HaveCount(2);

        var task1Metrics = profile.TaskMetrics.First(t => t.TaskId == "task1");
        task1Metrics.ExecutionTimeMs.Should().Be(0); // Missing end time defaults to 0

        var task2Metrics = profile.TaskMetrics.First(t => t.TaskId == "task2");
        task2Metrics.ExecutionTimeMs.Should().Be(200); // 320ms - 120ms
    }

    [Fact]
    public async Task GenerateProfileAsync_DeterministicBehavior_SameInputProducesSameOutput()
    {
        // Arrange
        const string executionId = "deterministic-test";
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var executionResult = new ExecutionResult(UserId.Anonymous, new[]
        {
            new TaskExecutionResult("task1", "Task 1", 1, TaskExecutionStatus.Succeeded, "Output 1"),
            new TaskExecutionResult("task2", "Task 2", 2, TaskExecutionStatus.Succeeded, "Output 2")
        });

        var auditLogs = new List<AuditLogEntry>
        {
            new(executionId, 1, baseTime, AuditEventType.OrchestratorStarted, null, "Execution started"),
            new(executionId, 2, baseTime.AddMilliseconds(10), AuditEventType.TaskStarted, "task1", "Task execution started"),
            new(executionId, 3, baseTime.AddMilliseconds(110), AuditEventType.TaskFinished, "task1", "Task executed and cached"),
            new(executionId, 4, baseTime.AddMilliseconds(120), AuditEventType.TaskStarted, "task2", "Task execution started"),
            new(executionId, 5, baseTime.AddMilliseconds(320), AuditEventType.TaskFinished, "task2", "Task executed and cached"),
            new(executionId, 6, baseTime.AddMilliseconds(330), AuditEventType.OrchestratorFinished, null, "Execution finished")
        };

        _mockResultStore.GetAsync(executionId).Returns(executionResult);
        _mockAuditLogger.GetExecutionLogsAsync(executionId).Returns(auditLogs);

        // Act
        var profile1 = await _profiler.GenerateProfileAsync(executionId);
        var profile2 = await _profiler.GenerateProfileAsync(executionId);

        // Assert
        profile1.Should().NotBeNull();
        profile2.Should().NotBeNull();

        // Verify deterministic behavior (excluding ProfileGeneratedAtUtc which will differ)
        profile1!.ExecutionId.Should().Be(profile2!.ExecutionId);
        profile1.TaskMetrics.Should().BeEquivalentTo(profile2.TaskMetrics);
        profile1.CriticalPath.Should().BeEquivalentTo(profile2.CriticalPath);
        profile1.ResourceUtilization.Should().BeEquivalentTo(profile2.ResourceUtilization);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GenerateProfileAsync_WithInvalidExecutionId_ThrowsArgumentException(string? executionId)
    {
        // Act & Assert
        await FluentActions.Invoking(() => _profiler.GenerateProfileAsync(executionId!))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("ExecutionId cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_WithNullResultStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        FluentActions.Invoking(() => new DeterministicExecutionProfiler(null!, _mockAuditLogger))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("resultStore");
    }

    [Fact]
    public void Constructor_WithNullAuditLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        FluentActions.Invoking(() => new DeterministicExecutionProfiler(_mockResultStore, null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("auditLogger");
    }
}