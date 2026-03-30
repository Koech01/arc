using NSubstitute;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Identity;
using Arc.Application.Execution;
using Arc.Application.Telemetry;
using Arc.Application.Notifications;


namespace Arc.UnitTests.Execution;
public sealed class DeterministicExecutionEngineTests
{
    [Fact]
    public void Execute_SameGraph_AlwaysProducesSameOrder()
    {
        // Arrange
        var graph = new ExecutionGraph(new[]
        {
            new TaskNode("B", "Task B", null, null, new[] { "A" }),
            new TaskNode("A", "Task A", null, null, Array.Empty<string>())
        });

        // Substitute IAgentExecutor for deterministic engine
        var agentExecutor = Substitute.For<IAgentExecutor>();
        var auditLogger = Substitute.For<IAuditLogger>();
        var cache = Substitute.For<ITaskExecutionCache>();
        var userContext = Substitute.For<IUserContext>();
        var webhookDispatcher = Substitute.For<Arc.Application.Webhooks.IWebhookDispatcher>();
        var notificationService = Substitute.For<INotificationService>();
        userContext.CurrentUserId.Returns(UserId.Anonymous);

        // Configure agentExecutor to return deterministic TaskExecutionResult
        agentExecutor.ExecuteAsync(Arg.Any<TaskNode>(), Arg.Any<IReadOnlyDictionary<string, string>>()).Returns(ci =>
        {
            var task = ci.Arg<TaskNode>();
            return Task.FromResult(
                new TaskExecutionResult(
                    task.Id,
                    task.Name,
                    1,
                    TaskExecutionStatus.Succeeded,
                    ""
                )
            );
        });

        var engine = new DeterministicExecutionEngineV1(agentExecutor, auditLogger, cache, userContext, webhookDispatcher, notificationService);

        // Act
        var first = engine.Execute(graph);
        var second = engine.Execute(graph);

        // Assert
        first.Tasks.Should().BeEquivalentTo(
            second.Tasks,
            options => options.WithStrictOrdering()
        );
    }
}