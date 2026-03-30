using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Execution;
using Arc.Infrastructure.Execution;


public sealed class TaskCacheDeterminismTests
{
    [Fact]
    public async Task SameTask_ShouldReturnCachedResult()
    {
        var cache = new SqliteTaskExecutionCache(":memory:");
        var node = new TaskNode("A", "TaskA", null, null, Array.Empty<string>());
        var result = new Arc.Application.Results.TaskExecutionResult("A", "TaskA", 1,
            Arc.Application.Results.TaskExecutionStatus.Succeeded, "");

        var hash = DeterministicTaskHasher.Compute(node, Array.Empty<Arc.Application.Results.TaskExecutionResult>());

        await cache.StoreAsync(hash, result, DateTime.UtcNow.AddMinutes(5));

        var loaded = await cache.GetAsync(hash);

        loaded.Should().NotBeNull();
        loaded!.TaskId.Should().Be("A");
    }
}