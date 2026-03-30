using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;
namespace Arc.UnitTests.Execution;
using Arc.Infrastructure.Execution;


/// <summary>
/// Unit tests for execution filtering and analytics.
/// Tests both InMemoryExecutionResultStore and deterministic query behavior.
/// </summary>
public sealed class ExecutionFilteringAndAnalyticsTests
{
    [Fact]
    public async Task QueryAsync_WithNoFilter_ReturnsAllExecutions()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var tasks1 = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out") };
        var tasks2 = new[] { new TaskExecutionResult("task-2", "Task 2", 1, TaskExecutionStatus.Succeeded, "out") };

        await store.StoreAsync("exec-1", new ExecutionResult(UserId.Anonymous, tasks1));
        await store.StoreAsync("exec-2", new ExecutionResult(UserId.Anonymous, tasks2));

        // Act
        var result = await store.QueryAsync(null, new PaginationParams(10, 0));

        // Assert
        result.Executions.Should().HaveCount(2);
        result.Analytics.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task QueryAsync_FilterByStatus_ReturnOnlyMatching()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var succeededTasks = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out") };
        var failedTasks = new[]
        {
            new TaskExecutionResult("task-2", "Task 2", 1, TaskExecutionStatus.Failed, "error")
        };

        await store.StoreAsync("exec-success", new ExecutionResult(UserId.Anonymous, succeededTasks));
        await store.StoreAsync("exec-failed", new ExecutionResult(UserId.Anonymous, failedTasks));

        var filter = new ExecutionQueryFilter(Status: "Succeeded", null, null, null, null, null, null, null);

        // Act
        var result = await store.QueryAsync(filter, new PaginationParams(10, 0));

        // Assert
        result.Executions.Should().HaveCount(1);
        result.Executions.First().Status.Should().Be("Succeeded");
    }

    [Fact]
    public async Task QueryAsync_FilterByDateRange_ReturnsOnlyInRange()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var tasks = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out") };

        // Store at different times
        await store.StoreAsync("exec-1", new ExecutionResult(UserId.Anonymous, tasks));
        await Task.Delay(100);
        var midPoint = DateTime.UtcNow;
        await Task.Delay(100);
        await store.StoreAsync("exec-2", new ExecutionResult(UserId.Anonymous, tasks));

        var filter = new ExecutionQueryFilter(
            Status: null,
            StartDateUtc: midPoint,
            EndDateUtc: DateTime.UtcNow.AddSeconds(1),
            null, null, null, null, null);

        // Act
        var result = await store.QueryAsync(filter, new PaginationParams(10, 0));

        // Assert
        result.Executions.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueryAsync_FilterByTaskCount_ReturnsOnlyMatching()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var singleTask = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out") };
        var multipleTasks = new[]
        {
            new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out"),
            new TaskExecutionResult("task-2", "Task 2", 2, TaskExecutionStatus.Succeeded, "out"),
            new TaskExecutionResult("task-3", "Task 3", 3, TaskExecutionStatus.Succeeded, "out")
        };

        await store.StoreAsync("exec-single", new ExecutionResult(UserId.Anonymous, singleTask));
        await store.StoreAsync("exec-multiple", new ExecutionResult(UserId.Anonymous, multipleTasks));

        var filter = new ExecutionQueryFilter(null, null, null, MinTaskCount: 2, MaxTaskCount: 5, null, null, null);

        // Act
        var result = await store.QueryAsync(filter, new PaginationParams(10, 0));

        // Assert
        result.Executions.Should().HaveCount(1);
        result.Executions.First().TaskCount.Should().Be(3);
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var tasks = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out") };

        for (int i = 0; i < 25; i++)
        {
            await store.StoreAsync($"exec-{i:D2}", new ExecutionResult(UserId.Anonymous, tasks));
        }

        // Act - Get second page (limit 10, offset 10)
        var result = await store.QueryAsync(null, new PaginationParams(10, 10));

        // Assert
        result.Executions.Should().HaveCount(10);
        result.Offset.Should().Be(10);
        result.Limit.Should().Be(10);
        result.TotalAvailable.Should().Be(25);
    }

    [Fact]
    public async Task QueryAsync_DeterministicOrdering()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var tasks = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out") };

        // Store in random order
        await store.StoreAsync("exec-z", new ExecutionResult(UserId.Anonymous, tasks));
        await store.StoreAsync("exec-a", new ExecutionResult(UserId.Anonymous, tasks));
        await store.StoreAsync("exec-m", new ExecutionResult(UserId.Anonymous, tasks));

        // Act - Query multiple times
        var result1 = await store.QueryAsync(null, new PaginationParams(10, 0));
        var result2 = await store.QueryAsync(null, new PaginationParams(10, 0));

        // Assert - Order must be identical and sorted by ExecutionId
        result1.Executions.Select(e => e.ExecutionId)
            .Should()
            .Equal(result2.Executions.Select(e => e.ExecutionId));

        result1.Executions.Select(e => e.ExecutionId)
            .Should()
            .Equal("exec-a", "exec-m", "exec-z");
    }

    [Fact]
    public async Task QueryAsync_CalculatesAnalyticsCorrectly()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var successTasks = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out") };
        var failedTasks = new[] { new TaskExecutionResult("task-2", "Task 2", 1, TaskExecutionStatus.Failed, "error") };

        await store.StoreAsync("exec-1", new ExecutionResult(UserId.Anonymous, successTasks));
        await store.StoreAsync("exec-2", new ExecutionResult(UserId.Anonymous, failedTasks));
        await store.StoreAsync("exec-3", new ExecutionResult(UserId.Anonymous, successTasks));

        // Act
        var result = await store.QueryAsync(null, new PaginationParams(10, 0));

        // Assert
        result.Analytics.TotalCount.Should().Be(3);
        result.Analytics.SuccessCount.Should().Be(2);
        result.Analytics.FailureCount.Should().Be(1);
        result.Analytics.SuccessRate.Should().BeApproximately(0.6667, 0.01);
    }

    [Fact]
    public async Task QueryAsync_CalculatesAverageMetrics()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var tasks2 = new[]
        {
            new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out"),
            new TaskExecutionResult("task-2", "Task 2", 2, TaskExecutionStatus.Succeeded, "out")
        };
        var tasks3 = new[]
        {
            new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out"),
            new TaskExecutionResult("task-2", "Task 2", 2, TaskExecutionStatus.Succeeded, "out"),
            new TaskExecutionResult("task-3", "Task 3", 3, TaskExecutionStatus.Succeeded, "out")
        };

        await store.StoreAsync("exec-1", new ExecutionResult(UserId.Anonymous, tasks2));
        await store.StoreAsync("exec-2", new ExecutionResult(UserId.Anonymous, tasks3));

        // Act
        var result = await store.QueryAsync(null, new PaginationParams(10, 0));

        // Assert
        result.Analytics.AverageTaskCount.Should().Be(2); // (2+3)/2 = 2.5 -> 2 (integer division)
    }

    [Fact]
    public async Task QueryAsync_CombinesMultipleFilters()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var tasks = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out") };

        await store.StoreAsync("exec-1", new ExecutionResult(UserId.Anonymous, tasks));
        await store.StoreAsync("exec-2", new ExecutionResult(UserId.Anonymous, tasks));

        var filter = new ExecutionQueryFilter(
            Status: "Succeeded",
            StartDateUtc: DateTime.UtcNow.AddHours(-1),
            EndDateUtc: DateTime.UtcNow.AddHours(1),
            MinTaskCount: 1,
            MaxTaskCount: 5,
            null, null, null);

        // Act
        var result = await store.QueryAsync(filter, new PaginationParams(10, 0));

        // Assert
        result.Executions.Should().HaveCount(2);
        result.Analytics.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task QueryAsync_EmptyResultsReturnCorrectAnalytics()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();

        var filter = new ExecutionQueryFilter(Status: "Succeeded", null, null, null, null, null, null, null);

        // Act
        var result = await store.QueryAsync(filter, new PaginationParams(10, 0));

        // Assert
        result.Executions.Should().BeEmpty();
        result.Analytics.TotalCount.Should().Be(0);
        result.Analytics.SuccessRate.Should().Be(0);
    }

    [Fact]
    public async Task QueryAsync_WithIncludeArchived_ReturnsArchivedRowWithIsArchivedTrue()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var tasks = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out") };

        await store.StoreAsync("exec-active", new ExecutionResult(UserId.Anonymous, tasks));
        await store.StoreAsync("exec-archived", new ExecutionResult(UserId.Anonymous, tasks));
        await store.ArchiveAsync("exec-archived", Guid.NewGuid());

        var filter = new ExecutionQueryFilter(null, null, null, null, null, null, null, IncludeArchived: true);

        // Act
        var result = await store.QueryAsync(filter, new PaginationParams(10, 0));

        // Assert
        result.Executions.Should().HaveCount(2);
        var archived = result.Executions.Single(e => e.ExecutionId == "exec-archived");
        archived.IsArchived.Should().BeTrue();
        var active = result.Executions.Single(e => e.ExecutionId == "exec-active");
        active.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task QueryAsync_DefaultFilter_ExcludesArchivedRows()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var tasks = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out") };

        await store.StoreAsync("exec-active", new ExecutionResult(UserId.Anonymous, tasks));
        await store.StoreAsync("exec-archived", new ExecutionResult(UserId.Anonymous, tasks));
        await store.ArchiveAsync("exec-archived", Guid.NewGuid());

        // Act - null filter (no includeArchived) and explicit false both exclude archived
        var resultNull = await store.QueryAsync(null, new PaginationParams(10, 0));
        var filterFalse = new ExecutionQueryFilter(null, null, null, null, null, null, null, IncludeArchived: false);
        var resultFalse = await store.QueryAsync(filterFalse, new PaginationParams(10, 0));

        // Assert
        resultNull.Executions.Should().HaveCount(1);
        resultNull.Executions.Single().ExecutionId.Should().Be("exec-active");

        resultFalse.Executions.Should().HaveCount(1);
        resultFalse.Executions.Single().ExecutionId.Should().Be("exec-active");
    }

    [Fact]
    public async Task UnarchiveAsync_MakesArchivedExecutionVisibleAgain()
    {
        // Arrange
        var store = new InMemoryExecutionResultStore();
        var tasks = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "out") };
        await store.StoreAsync("exec-1", new ExecutionResult(UserId.Anonymous, tasks));
        await store.ArchiveAsync("exec-1", Guid.NewGuid());

        // Verify archived
        var afterArchive = await store.QueryAsync(null, new PaginationParams(10, 0));
        afterArchive.Executions.Should().BeEmpty();

        // Act
        await store.UnarchiveAsync("exec-1", Guid.NewGuid());

        // Assert
        var afterUnarchive = await store.QueryAsync(null, new PaginationParams(10, 0));
        afterUnarchive.Executions.Should().HaveCount(1);
        afterUnarchive.Executions.Single().IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task PaginationParams_ValidateEnforcesLimits()
    {
        // Test limit constraints
        var validated1 = PaginationParams.Validate(2000, 0);
        validated1.Limit.Should().Be(1000);

        var validated2 = PaginationParams.Validate(0, 0);
        validated2.Limit.Should().Be(1);

        // Test offset constraints
        var validated3 = PaginationParams.Validate(10, -5);
        validated3.Offset.Should().Be(0);

        var validated4 = PaginationParams.Validate(10, 100);
        validated4.Offset.Should().Be(100);
    }
}