using NSubstitute;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Planning;
using Arc.Application.Execution;
using Arc.Application.Orchestration;


namespace Arc.UnitTests.Orchestration;
public sealed class DeterministicOrchestratorTests
{
    [Fact]
    public void Execute_DelegatesToPlannerAndExecutionEngine()
    {
        var planner = Substitute.For<IPlanner>();
        var engine = Substitute.For<IExecutionEngine>();

        var graph = new ExecutionGraph(new[]
        {
            new TaskNode("A", "Task A", dependsOn: Array.Empty<string>())
        });

        planner.Plan("input").Returns(graph);

        engine.Execute(graph).Returns(
            new Arc.Application.Results.ExecutionResult(Arc.Domain.Models.UserId.Anonymous, new[]
            {
                new Arc.Application.Results.TaskExecutionResult(
                    "A", "Task A", 1,
                    Arc.Application.Results.TaskExecutionStatus.Succeeded, "")
            })
        );

        var orchestrator = new DeterministicOrchestratorV1(planner, engine);

        var result = orchestrator.Execute("input");

        result.Tasks.Should().HaveCount(1);
        planner.Received(1).Plan("input");
        engine.Received(1).Execute(graph);
    }
}