using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Planning;
namespace Arc.UnitTests.Planning;


public sealed class DeterministicPlannerV1Tests
{
    private readonly IPlanner _planner = new DeterministicPlannerV1();

    [Fact]
    public void Plan_SameInput_ProducesIdenticalGraphs()
    {
        // Arrange
        var input = """
            Fetch data
            Process data
            Generate report
            """;

        // Act
        var graph1 = _planner.Plan(input);
        var graph2 = _planner.Plan(input);

        // Assert
        graph1.Nodes.Should().HaveCount(3);
        graph2.Nodes.Should().HaveCount(3);

        graph1.Nodes.Select(n => n.Id)
            .Should()
            .Equal(graph2.Nodes.Select(n => n.Id));

        graph1.Nodes.Select(n => n.Name)
            .Should()
            .Equal(graph2.Nodes.Select(n => n.Name));

        graph1.Nodes.Select(n => n.DependsOn.SingleOrDefault())
            .Should()
            .Equal(graph2.Nodes.Select(n => n.DependsOn.SingleOrDefault()));
    }

    [Fact]
    public void Plan_TrimsAndIgnoresEmptyLines_Deterministically()
    {
        // Arrange
        var input = "\n  Task A  \n\n Task B \n";

        // Act
        var graph = _planner.Plan(input);

        // Assert
        graph.Nodes.Should().HaveCount(2);

        graph.Nodes.Select(n => n.Id)
            .Should()
            .Equal("task-1", "task-2");

        graph.Nodes.Select(n => n.Name)
            .Should()
            .Equal("Task A", "Task B");
    }
}