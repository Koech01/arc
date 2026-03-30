using NSubstitute;
using Arc.Domain.Models;
using FluentAssertions;
using Arc.Application.Results;
using Arc.Application.Execution;
namespace Arc.UnitTests.Execution;
using Arc.Infrastructure.Execution;
using Arc.Application.Orchestration;


/// <summary>
/// Unit tests for DeterministicBatchExecutorV1.
/// Verifies deterministic batch processing, metrics aggregation, and error handling.
/// </summary>
public sealed class DeterministicBatchExecutorV1Tests
{
    [Fact]
    public async Task ExecuteBatchAsync_WithValidInputs_ExecutesAllAndReturnsResults()
    {
        // Arrange
        var inputs = new[] { "task-a", "task-b", "task-c" };
        var tasks = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "output-a")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);

        var orchestrator = Substitute.For<IOrchestrator>();
        orchestrator.Execute(Arg.Any<string>()).Returns(result);

        var resultStore = Substitute.For<IExecutionResultStore>();
        var executor = new DeterministicBatchExecutorV1(orchestrator, resultStore);

        // Act
        var batchResult = await executor.ExecuteBatchAsync(inputs);

        // Assert
        batchResult.Should().NotBeNull();
        batchResult.Executions.Should().HaveCount(3);
        batchResult.SuccessCount.Should().Be(3);
        batchResult.FailureCount.Should().Be(0);
        orchestrator.Received(3).Execute(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteBatchAsync_GeneratesDeterministicBatchId()
    {
        // Arrange
        var inputs = new[] { "input-1", "input-2" };
        var tasks = new[]
        {
            new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);

        var orchestrator = Substitute.For<IOrchestrator>();
        orchestrator.Execute(Arg.Any<string>()).Returns(result);

        var resultStore = Substitute.For<IExecutionResultStore>();
        var executor = new DeterministicBatchExecutorV1(orchestrator, resultStore);

        // Act - Execute batch twice
        var batch1 = await executor.ExecuteBatchAsync(inputs);
        var batch2 = await executor.ExecuteBatchAsync(inputs);

        // Assert - Batch IDs must be identical for same inputs
        batch1.BatchId.Should().Be(batch2.BatchId);
        batch1.BatchId.Should().StartWith("batch-");
    }

    [Fact]
    public async Task ExecuteBatchAsync_AggregatesMetricsCorrectly()
    {
        // Arrange
        var inputs = new[] { "task-1", "task-2", "task-3" };
        var tasks = new[]
        {
            new TaskExecutionResult("task-x", "Task X", 1, TaskExecutionStatus.Succeeded, "output")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);

        var orchestrator = Substitute.For<IOrchestrator>();
        orchestrator.Execute(Arg.Any<string>()).Returns(result);

        var resultStore = Substitute.For<IExecutionResultStore>();
        var executor = new DeterministicBatchExecutorV1(orchestrator, resultStore);

        // Act
        var batchResult = await executor.ExecuteBatchAsync(inputs);

        // Assert
        batchResult.TotalExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
        batchResult.AverageExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
        batchResult.SuccessCount.Should().Be(3);
        batchResult.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteBatchAsync_PreservesExecutionOrder()
    {
        // Arrange
        var inputs = new[] { "input-a", "input-b", "input-c" };
        var tasks = new[]
        {
            new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);

        var orchestrator = Substitute.For<IOrchestrator>();
        orchestrator.Execute(Arg.Any<string>()).Returns(result);

        var resultStore = Substitute.For<IExecutionResultStore>();
        var executor = new DeterministicBatchExecutorV1(orchestrator, resultStore);

        // Act
        var batchResult = await executor.ExecuteBatchAsync(inputs);

        // Assert - Execution order must match input order
        batchResult.Executions[0].Index.Should().Be(0);
        batchResult.Executions[1].Index.Should().Be(1);
        batchResult.Executions[2].Index.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteBatchAsync_MarkStatusSucceededWhenAllTasksSucceed()
    {
        // Arrange
        var inputs = new[] { "task-1" };
        var tasks = new[]
        {
            new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);

        var orchestrator = Substitute.For<IOrchestrator>();
        orchestrator.Execute(Arg.Any<string>()).Returns(result);

        var resultStore = Substitute.For<IExecutionResultStore>();
        var executor = new DeterministicBatchExecutorV1(orchestrator, resultStore);

        // Act
        var batchResult = await executor.ExecuteBatchAsync(inputs);

        // Assert
        batchResult.Executions[0].Status.Should().Be("Succeeded");
    }

    [Fact]
    public async Task ExecuteBatchAsync_MarkStatusPartiallyFailedWhenSomeTasksFail()
    {
        // Arrange
        var inputs = new[] { "task-mixed" };
        var tasks = new[]
        {
            new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output"),
            new TaskExecutionResult("task-2", "Task 2", 2, TaskExecutionStatus.Failed, "error")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);

        var orchestrator = Substitute.For<IOrchestrator>();
        orchestrator.Execute(Arg.Any<string>()).Returns(result);

        var resultStore = Substitute.For<IExecutionResultStore>();
        var executor = new DeterministicBatchExecutorV1(orchestrator, resultStore);

        // Act
        var batchResult = await executor.ExecuteBatchAsync(inputs);

        // Assert
        batchResult.Executions[0].Status.Should().Be("PartiallyFailed");
        batchResult.SuccessCount.Should().Be(0);
        batchResult.FailureCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteBatchAsync_HandlesOrchestrationExceptionsGracefully()
    {
        // Arrange
        var inputs = new[] { "task-1", "task-error", "task-3" };
        var tasks = new[]
        {
            new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);

        var orchestrator = Substitute.For<IOrchestrator>();
        orchestrator.Execute("task-1").Returns(result);
        orchestrator.Execute("task-error").Returns(x => throw new InvalidOperationException("Orchestration failed"));
        orchestrator.Execute("task-3").Returns(result);

        var resultStore = Substitute.For<IExecutionResultStore>();
        var executor = new DeterministicBatchExecutorV1(orchestrator, resultStore);

        // Act
        var batchResult = await executor.ExecuteBatchAsync(inputs);

        // Assert
        batchResult.Executions.Should().HaveCount(3);
        batchResult.Executions[1].Status.Should().Contain("Failed");
        batchResult.FailureCount.Should().Be(1);
        batchResult.SuccessCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteBatchAsync_StoresEachExecutionResult()
    {
        // Arrange
        var inputs = new[] { "task-1", "task-2" };
        var tasks = new[]
        {
            new TaskExecutionResult("task-x", "Task X", 1, TaskExecutionStatus.Succeeded, "output")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);

        var orchestrator = Substitute.For<IOrchestrator>();
        orchestrator.Execute(Arg.Any<string>()).Returns(result);

        var resultStore = Substitute.For<IExecutionResultStore>();
        var executor = new DeterministicBatchExecutorV1(orchestrator, resultStore);

        // Act
        await executor.ExecuteBatchAsync(inputs);

        // Assert - Each execution should be stored
        await resultStore.Received(2).StoreAsync(Arg.Any<string>(), Arg.Any<ExecutionResult>());
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithNullInputs_ThrowsArgumentException()
    {
        // Arrange
        var orchestrator = Substitute.For<IOrchestrator>();
        var resultStore = Substitute.For<IExecutionResultStore>();
        var executor = new DeterministicBatchExecutorV1(orchestrator, resultStore);

        // Act & Assert
        await executor.Invoking(e => e.ExecuteBatchAsync(null!))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithEmptyInputs_ThrowsArgumentException()
    {
        // Arrange
        var orchestrator = Substitute.For<IOrchestrator>();
        var resultStore = Substitute.For<IExecutionResultStore>();
        var executor = new DeterministicBatchExecutorV1(orchestrator, resultStore);

        // Act & Assert
        await executor.Invoking(e => e.ExecuteBatchAsync(Array.Empty<string>()))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteBatchAsync_IncludeCreationTimestamp()
    {
        // Arrange
        var inputs = new[] { "task-1" };
        var tasks = new[]
        {
            new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);

        var orchestrator = Substitute.For<IOrchestrator>();
        orchestrator.Execute(Arg.Any<string>()).Returns(result);

        var resultStore = Substitute.For<IExecutionResultStore>();
        var executor = new DeterministicBatchExecutorV1(orchestrator, resultStore);

        var beforeExecution = DateTime.UtcNow;

        // Act
        var batchResult = await executor.ExecuteBatchAsync(inputs);

        var afterExecution = DateTime.UtcNow;

        // Assert
        batchResult.CreatedAtUtc.Should().BeOnOrAfter(beforeExecution);
        batchResult.CreatedAtUtc.Should().BeOnOrBefore(afterExecution);
    }
}
