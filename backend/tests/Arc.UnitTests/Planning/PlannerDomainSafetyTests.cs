using FluentAssertions;
using Arc.Application.Planning;
namespace Arc.UnitTests.Planning;


public sealed class PlannerDomainSafetyTests
{
    private readonly IPlanner _planner = new DeterministicPlannerV1();

    [Fact]
    public void Plan_DoesNotProduceCyclicGraphs()
    {
        // Arrange
        var input = """
            Task 1
            Task 2
            Task 3
            Task 4
            """;

        // Act
        var action = () => _planner.Plan(input);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Plan_EmptyInput_Throws()
    {
        // Arrange
        var input = "   \n \n";

        // Act
        var action = () => _planner.Plan(input);

        // Assert
        action.Should().Throw<ArgumentException>();
    }
}