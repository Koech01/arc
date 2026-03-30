using FluentAssertions;
using Arc.Application.Planning;
namespace Arc.UnitTests.Planning;


public sealed class ExecutionGraphStructureTests
{
    private readonly IPlanner _planner = new DeterministicPlannerV1();

    [Fact]
    public void Plan_CreatesSequentialDependencies()
    {
        // Arrange
        var input = """
            Step one
            Step two
            Step three
            """;

        // Act
        var graph = _planner.Plan(input);
        var nodes = graph.Nodes.OrderBy(n => n.Id).ToArray();

        // Assert
        nodes[0].DependsOn.Should().BeEmpty();
        nodes[1].DependsOn.Should().ContainSingle("task-1");
        nodes[2].DependsOn.Should().ContainSingle("task-2");
    }

    [Fact]
    public void Plan_AssignsStableIds()
    {
        // Arrange
        var input = """
            Alpha
            Beta
            Gamma
            """;

        // Act
        var graph = _planner.Plan(input);

        // Assert
        graph.Nodes.Select(n => n.Id)
            .Should()
            .Equal("task-1", "task-2", "task-3");
    }
}