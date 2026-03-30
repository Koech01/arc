using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Results;
namespace Arc.UnitTests.Execution;
using Arc.Infrastructure.Execution;


public class SqliteExecutionResultStoreTests
{
    private const string TestDbPath = "./data/test_execution_results.db";

    [Fact]
    public async Task StoreAndRetrieveExecutionResult_ShouldBeDeterministic()
    {
        if (File.Exists(TestDbPath))
            File.Delete(TestDbPath);

        var store = new SqliteExecutionResultStore(TestDbPath);

        var executionResult = new ExecutionResult(
            UserId.Anonymous,
            new[]
            {
                new TaskExecutionResult("task1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output1"),
                new TaskExecutionResult("task2", "Task 2", 2, TaskExecutionStatus.Succeeded, "output2")
            }
        );

        await store.StoreAsync("exec-123", executionResult);
        var retrieved = await store.GetAsync("exec-123");

        retrieved.Should().NotBeNull();
        retrieved!.Tasks.Should().HaveCount(2);
        retrieved.Tasks.ElementAt(0).Output.Should().Be("output1");
        retrieved.Tasks.ElementAt(1).Output.Should().Be("output2");
    }
}