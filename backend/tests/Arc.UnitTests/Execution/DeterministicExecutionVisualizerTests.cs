using NSubstitute;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.Telemetry;
namespace Arc.UnitTests.Execution;
using Arc.Infrastructure.Execution;

public sealed class DeterministicExecutionVisualizerTests
{
    private readonly IExecutionResultStore _mockResultStore;
    private readonly IAuditLogger _mockAuditLogger;
    private readonly IExecutionProfiler _mockProfiler;
    private readonly DeterministicExecutionVisualizer _visualizer;

    public DeterministicExecutionVisualizerTests()
    {
        _mockResultStore = Substitute.For<IExecutionResultStore>();
        _mockAuditLogger = Substitute.For<IAuditLogger>();
        _mockProfiler = Substitute.For<IExecutionProfiler>();
        _visualizer = new DeterministicExecutionVisualizer(_mockResultStore, _mockAuditLogger, _mockProfiler);
    }

    [Fact]
    public async Task GenerateVisualizationAsync_WithValidExecution_ReturnsVisualization()
    {
        // Arrange
        const string executionId = "test-execution-id";
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
            new(executionId, 3, baseTime.AddMilliseconds(110), AuditEventType.TaskFinished, "task1", "Task executed"),
            new(executionId, 4, baseTime.AddMilliseconds(120), AuditEventType.TaskStarted, "task2", "Task execution started"),
            new(executionId, 5, baseTime.AddMilliseconds(320), AuditEventType.TaskFinished, "task2", "Task executed")
        };

        var profile = new ExecutionPerformanceProfile(
            executionId,
            new[]
            {
                new TaskPerformanceMetrics("task1", "Task 1", 1, 100, 0, true, new List<string>()),
                new TaskPerformanceMetrics("task2", "Task 2", 2, 200, 10, true, new List<string> { "task1" })
            },
            new CriticalPathAnalysis(new[] { "task1", "task2" }, 300, 100.0),
            new ResourceUtilizationMetrics(300, 0, 300, 0.0, 1, 150.0),
            DateTime.UtcNow
        );

        _mockResultStore.GetAsync(executionId).Returns(executionResult);
        _mockAuditLogger.GetExecutionLogsAsync(executionId).Returns(auditLogs);
        _mockProfiler.GenerateProfileAsync(executionId).Returns(profile);

        // Act
        var visualization = await _visualizer.GenerateVisualizationAsync(executionId);

        // Assert
        visualization.Should().NotBeNull();
        visualization!.ExecutionId.Should().Be(executionId);
        
        // Verify dependency graph
        visualization.DependencyGraph.Should().HaveCount(2);
        var task1Node = visualization.DependencyGraph.First(n => n.TaskId == "task1");
        task1Node.TaskName.Should().Be("Task 1");
        task1Node.ExecutionOrder.Should().Be(1);
        task1Node.Status.Should().Be("Succeeded");
        task1Node.Dependencies.Should().BeEmpty();
        task1Node.IsCriticalPath.Should().BeTrue();
        task1Node.ExecutionTimeMs.Should().Be(100);

        var task2Node = visualization.DependencyGraph.First(n => n.TaskId == "task2");
        task2Node.Dependencies.Should().BeEquivalentTo(new[] { "task1" });
        task2Node.IsCriticalPath.Should().BeTrue();

        // Verify execution timeline
        visualization.ExecutionTimeline.Should().HaveCount(2);
        var task1Event = visualization.ExecutionTimeline.First(e => e.TaskId == "task1");
        task1Event.StartTime.Should().Be(baseTime.AddMilliseconds(10));
        task1Event.EndTime.Should().Be(baseTime.AddMilliseconds(110));
        task1Event.DurationMs.Should().Be(100);
        task1Event.EventType.Should().Be("TaskExecution");
        task1Event.IsCriticalPath.Should().BeTrue();

        // Verify critical path
        visualization.CriticalPathTaskIds.Should().BeEquivalentTo(new[] { "task1", "task2" });

        // Verify resource allocation
        visualization.ResourceAllocation.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateVisualizationAsync_WithNonExistentExecution_ReturnsNull()
    {
        // Arrange
        const string executionId = "non-existent-id";
        _mockResultStore.GetAsync(executionId).Returns((ExecutionResult?)null);

        // Act
        var visualization = await _visualizer.GenerateVisualizationAsync(executionId);

        // Assert
        visualization.Should().BeNull();
    }

    [Fact]
    public async Task GenerateVisualizationAsync_WithEmptyAuditLogs_ReturnsNull()
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
        var visualization = await _visualizer.GenerateVisualizationAsync(executionId);

        // Assert
        visualization.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GenerateVisualizationAsync_WithInvalidExecutionId_ThrowsArgumentException(string? executionId)
    {
        // Act & Assert
        await FluentActions.Invoking(() => _visualizer.GenerateVisualizationAsync(executionId!))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("ExecutionId cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_WithNullResultStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        FluentActions.Invoking(() => new DeterministicExecutionVisualizer(null!, _mockAuditLogger, _mockProfiler))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("resultStore");
    }

    [Fact]
    public void Constructor_WithNullAuditLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        FluentActions.Invoking(() => new DeterministicExecutionVisualizer(_mockResultStore, null!, _mockProfiler))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("auditLogger");
    }

    [Fact]
    public void Constructor_WithNullProfiler_ThrowsArgumentNullException()
    {
        // Act & Assert
        FluentActions.Invoking(() => new DeterministicExecutionVisualizer(_mockResultStore, _mockAuditLogger, null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("profiler");
    }
}