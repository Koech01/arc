using NSubstitute;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.LLM;
using Arc.Application.Results;
using Arc.Application.Execution;
using Microsoft.Extensions.Logging;


public class DeterministicAgentExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_SameTask_ProducesSameResult()
    {
        var llmProvider = Substitute.For<ILLMProvider>();
        var llmProviderService = Substitute.For<ILLMProviderService>();
        var logger = Substitute.For<ILogger<DeterministicAgentExecutorV1>>();
        llmProvider.GenerateTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult("deterministic output"));
        llmProviderService.GetProviderAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(llmProvider);

        var executor = new DeterministicAgentExecutorV1(llmProviderService, logger);
        var task = new TaskNode("task-1", "Test Task");
        var deps = new Dictionary<string, string>();

        var result1 = await executor.ExecuteAsync(task, deps);
        var result2 = await executor.ExecuteAsync(task, deps);

        result1.Should().BeEquivalentTo(result2);
        result1.Output.Should().Be("deterministic output");
    }

    [Fact]
    public async Task ExecuteAsync_ResultPropertiesAreCorrect()
    {
        var llmProvider = Substitute.For<ILLMProvider>();
        var llmProviderService = Substitute.For<ILLMProviderService>();
        var logger = Substitute.For<ILogger<DeterministicAgentExecutorV1>>();
        llmProvider.GenerateTextAsync("Execute the following task: Compute", Arg.Any<CancellationToken>()).Returns(Task.FromResult("task output"));
        llmProviderService.GetProviderAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(llmProvider);

        var executor = new DeterministicAgentExecutorV1(llmProviderService, logger);
        var task = new TaskNode("task-42", "Compute");
        var deps = new Dictionary<string, string>();

        var result = await executor.ExecuteAsync(task, deps);

        result.TaskId.Should().Be("task-42");
        result.TaskName.Should().Be("Compute");
        result.Status.Should().Be(TaskExecutionStatus.Succeeded);
        result.Output.Should().Be("task output");
    }

    [Fact]
    public async Task ExecuteAsync_LLMFailure_ReturnsFailedStatus()
    {
        var llmProvider = Substitute.For<ILLMProvider>();
        var llmProviderService = Substitute.For<ILLMProviderService>();
        var logger = Substitute.For<ILogger<DeterministicAgentExecutorV1>>();
        llmProvider.GenerateTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<string>(new Exception("LLM error")));
        llmProviderService.GetProviderAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(llmProvider);

        var executor = new DeterministicAgentExecutorV1(llmProviderService, logger);
        var task = new TaskNode("task-1", "Test Task");
        var deps = new Dictionary<string, string>();

        var result = await executor.ExecuteAsync(task, deps);

        result.Status.Should().Be(TaskExecutionStatus.Failed);
        result.Output.Should().Contain("LLM error");
    }

    [Fact]
    public async Task ExecuteAsync_CallsLLMWithCorrectPrompt()
    {
        var llmProvider = Substitute.For<ILLMProvider>();
        var llmProviderService = Substitute.For<ILLMProviderService>();
        var logger = Substitute.For<ILogger<DeterministicAgentExecutorV1>>();
        llmProvider.GenerateTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("output");
        llmProviderService.GetProviderAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(llmProvider);

        var executor = new DeterministicAgentExecutorV1(llmProviderService, logger);
        var task = new TaskNode("task-1", "Analyze data");
        var deps = new Dictionary<string, string>();

        await executor.ExecuteAsync(task, deps);

        await llmProvider.Received(1).GenerateTextAsync("Execute the following task: Analyze data", Arg.Any<CancellationToken>());
    }
}