using NSubstitute;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;
namespace Arc.UnitTests.Execution;
using Arc.Infrastructure.Execution;


/// <summary>
/// Unit tests for DeterministicExecutionComparer.
/// Verifies task-by-task comparison, diff metrics, and deterministic behavior.
/// </summary>
public sealed class DeterministicExecutionComparerTests
{
    [Fact]
    public async Task CompareAsync_WithIdenticalExecutions_ReturnsAllIdentical()
    {
        // Arrange
        var tasks = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "output-a"),
            new TaskExecutionResult("task-b", "Task B", 2, TaskExecutionStatus.Succeeded, "output-b")
        };
        var result = new ExecutionResult(UserId.Anonymous, tasks);

        var store = Substitute.For<IExecutionResultStore>();
        store.GetAsync("exec-1").Returns(result);
        store.GetAsync("exec-2").Returns(result);

        var comparer = new DeterministicExecutionComparer(store);

        // Act
        var comparison = await comparer.CompareAsync("exec-1", "exec-2");

        // Assert
        comparison.Should().NotBeNull();
        comparison!.TaskComparisons.Should().AllSatisfy(tc => tc.IsDifferent.Should().BeFalse());
        comparison.Metrics.IdenticalTasks.Should().Be(2);
        comparison.Metrics.DifferentTasks.Should().Be(0);
        comparison.Metrics.SimilarityPercentage.Should().Be(100);
    }

    [Fact]
    public async Task CompareAsync_WithDifferentStatuses_IdentifiesDifferences()
    {
        // Arrange
        var tasks1 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "output-a")
        };
        var tasks2 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Failed, "error-a")
        };

        var store = Substitute.For<IExecutionResultStore>();
        store.GetAsync("exec-1").Returns(new ExecutionResult(UserId.Anonymous, tasks1));
        store.GetAsync("exec-2").Returns(new ExecutionResult(UserId.Anonymous, tasks2));

        var comparer = new DeterministicExecutionComparer(store);

        // Act
        var comparison = await comparer.CompareAsync("exec-1", "exec-2");

        // Assert
        comparison.Should().NotBeNull();
        comparison!.TaskComparisons.Should().AllSatisfy(tc => tc.IsDifferent.Should().BeTrue());
        comparison.Metrics.DifferentTasks.Should().Be(1);
    }

    [Fact]
    public async Task CompareAsync_WithDifferentOutputs_IdentifiesDifferences()
    {
        // Arrange
        var tasks1 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "output-a")
        };
        var tasks2 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "output-different")
        };

        var store = Substitute.For<IExecutionResultStore>();
        store.GetAsync("exec-1").Returns(new ExecutionResult(UserId.Anonymous, tasks1));
        store.GetAsync("exec-2").Returns(new ExecutionResult(UserId.Anonymous, tasks2));

        var comparer = new DeterministicExecutionComparer(store);

        // Act
        var comparison = await comparer.CompareAsync("exec-1", "exec-2");

        // Assert
        comparison!.TaskComparisons.First().IsDifferent.Should().BeTrue();
        comparison.TaskComparisons.First().Output1.Should().Be("output-a");
        comparison.TaskComparisons.First().Output2.Should().Be("output-different");
    }

    [Fact]
    public async Task CompareAsync_WithDifferentExecutionOrder_IdentifiesDifferences()
    {
        // Arrange
        var tasks1 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "out"),
            new TaskExecutionResult("task-b", "Task B", 2, TaskExecutionStatus.Succeeded, "out")
        };
        var tasks2 = new[]
        {
            new TaskExecutionResult("task-b", "Task B", 1, TaskExecutionStatus.Succeeded, "out"),
            new TaskExecutionResult("task-a", "Task A", 2, TaskExecutionStatus.Succeeded, "out")
        };

        var store = Substitute.For<IExecutionResultStore>();
        store.GetAsync("exec-1").Returns(new ExecutionResult(UserId.Anonymous, tasks1));
        store.GetAsync("exec-2").Returns(new ExecutionResult(UserId.Anonymous, tasks2));

        var comparer = new DeterministicExecutionComparer(store);

        // Act
        var comparison = await comparer.CompareAsync("exec-1", "exec-2");

        // Assert
        comparison!.Metrics.SameExecutionOrder.Should().BeFalse();
    }

    [Fact]
    public async Task CompareAsync_WithMissingTasks_MarksAsMissing()
    {
        // Arrange
        var tasks1 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "out"),
            new TaskExecutionResult("task-b", "Task B", 2, TaskExecutionStatus.Succeeded, "out")
        };
        var tasks2 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "out")
        };

        var store = Substitute.For<IExecutionResultStore>();
        store.GetAsync("exec-1").Returns(new ExecutionResult(UserId.Anonymous, tasks1));
        store.GetAsync("exec-2").Returns(new ExecutionResult(UserId.Anonymous, tasks2));

        var comparer = new DeterministicExecutionComparer(store);

        // Act
        var comparison = await comparer.CompareAsync("exec-1", "exec-2");

        // Assert
        comparison!.Metrics.SameTaskCount.Should().BeFalse();
        comparison.TaskComparisons.Should().HaveCount(2);
        comparison.TaskComparisons.Last().Status2.Should().Be("Missing");
    }

    [Fact]
    public async Task CompareAsync_FindsDivergencePoint()
    {
        // Arrange - First task identical, second differs, third identical
        var tasks1 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "out-a"),
            new TaskExecutionResult("task-b", "Task B", 2, TaskExecutionStatus.Succeeded, "out-b"),
            new TaskExecutionResult("task-c", "Task C", 3, TaskExecutionStatus.Succeeded, "out-c")
        };
        var tasks2 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "out-a"),
            new TaskExecutionResult("task-b", "Task B", 2, TaskExecutionStatus.Failed, "error-b"),
            new TaskExecutionResult("task-c", "Task C", 3, TaskExecutionStatus.Succeeded, "out-c")
        };

        var store = Substitute.For<IExecutionResultStore>();
        store.GetAsync("exec-1").Returns(new ExecutionResult(UserId.Anonymous, tasks1));
        store.GetAsync("exec-2").Returns(new ExecutionResult(UserId.Anonymous, tasks2));

        var comparer = new DeterministicExecutionComparer(store);

        // Act
        var comparison = await comparer.CompareAsync("exec-1", "exec-2");

        // Assert
        comparison!.Metrics.DivergencePointIndex.Should().Be(1);
    }

    [Fact]
    public async Task CompareAsync_CalculatesSimilarityPercentage()
    {
        // Arrange - 2 identical, 1 different = 66.67%
        var tasks1 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "out"),
            new TaskExecutionResult("task-b", "Task B", 2, TaskExecutionStatus.Succeeded, "out"),
            new TaskExecutionResult("task-c", "Task C", 3, TaskExecutionStatus.Succeeded, "out")
        };
        var tasks2 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "out"),
            new TaskExecutionResult("task-b", "Task B", 2, TaskExecutionStatus.Failed, "error"),
            new TaskExecutionResult("task-c", "Task C", 3, TaskExecutionStatus.Succeeded, "out")
        };

        var store = Substitute.For<IExecutionResultStore>();
        store.GetAsync("exec-1").Returns(new ExecutionResult(UserId.Anonymous, tasks1));
        store.GetAsync("exec-2").Returns(new ExecutionResult(UserId.Anonymous, tasks2));

        var comparer = new DeterministicExecutionComparer(store);

        // Act
        var comparison = await comparer.CompareAsync("exec-1", "exec-2");

        // Assert
        comparison!.Metrics.SimilarityPercentage.Should().BeApproximately(66.67, 0.1);
    }

    [Fact]
    public async Task CompareAsync_GeneratesSummary()
    {
        // Arrange
        var tasks1 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "out")
        };
        var tasks2 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "out")
        };

        var store = Substitute.For<IExecutionResultStore>();
        store.GetAsync("exec-1").Returns(new ExecutionResult(UserId.Anonymous, tasks1));
        store.GetAsync("exec-2").Returns(new ExecutionResult(UserId.Anonymous, tasks2));

        var comparer = new DeterministicExecutionComparer(store);

        // Act
        var comparison = await comparer.CompareAsync("exec-1", "exec-2");

        // Assert
        comparison!.Summary.Should().Contain("identical");
    }

    [Fact]
    public async Task CompareAsync_WithNonExistentExecution_ReturnsNull()
    {
        // Arrange
        var store = Substitute.For<IExecutionResultStore>();
        store.GetAsync("exec-1").Returns((ExecutionResult?)null);

        var comparer = new DeterministicExecutionComparer(store);

        // Act
        var comparison = await comparer.CompareAsync("exec-1", "exec-2");

        // Assert
        comparison.Should().BeNull();
    }

    [Fact]
    public async Task CompareAsync_WithSameExecutionId_ThrowsArgumentException()
    {
        // Arrange
        var store = Substitute.For<IExecutionResultStore>();
        var comparer = new DeterministicExecutionComparer(store);

        // Act & Assert
        await comparer.Invoking(c => c.CompareAsync("exec-1", "exec-1"))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CompareAsync_WithNullExecutionId1_ThrowsArgumentException()
    {
        // Arrange
        var store = Substitute.For<IExecutionResultStore>();
        var comparer = new DeterministicExecutionComparer(store);

        // Act & Assert
        await comparer.Invoking(c => c.CompareAsync(null!, "exec-2"))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CompareAsync_WithEmptyExecutionId2_ThrowsArgumentException()
    {
        // Arrange
        var store = Substitute.For<IExecutionResultStore>();
        var comparer = new DeterministicExecutionComparer(store);

        // Act & Assert
        await comparer.Invoking(c => c.CompareAsync("exec-1", ""))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CompareAsync_DeterministicBehavior()
    {
        // Arrange
        var tasks1 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "out-a"),
            new TaskExecutionResult("task-b", "Task B", 2, TaskExecutionStatus.Failed, "error-b")
        };
        var tasks2 = new[]
        {
            new TaskExecutionResult("task-a", "Task A", 1, TaskExecutionStatus.Succeeded, "out-a"),
            new TaskExecutionResult("task-b", "Task B", 2, TaskExecutionStatus.Succeeded, "out-b")
        };

        var store = Substitute.For<IExecutionResultStore>();
        store.GetAsync("exec-1").Returns(new ExecutionResult(UserId.Anonymous, tasks1));
        store.GetAsync("exec-2").Returns(new ExecutionResult(UserId.Anonymous, tasks2));

        var comparer = new DeterministicExecutionComparer(store);

        // Act - Compare multiple times
        var comparison1 = await comparer.CompareAsync("exec-1", "exec-2");
        var comparison2 = await comparer.CompareAsync("exec-1", "exec-2");

        // Assert - Results must be identical
        comparison1!.Metrics.Should().Be(comparison2!.Metrics);
        comparison1.Summary.Should().Be(comparison2.Summary);
        comparison1.TaskComparisons
            .Should()
            .Equal(comparison2.TaskComparisons);
    }
}
