using FluentAssertions;
using Arc.Domain.Models;
using Arc.Domain.Exceptions;
namespace Arc.UnitTests.Workflows;


public sealed class WorkflowTaskTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesWorkflowTask()
    {
        var config = new Dictionary<string, string> { { "url", "https://example.com" } };
        var dependencies = new List<string> { "task1" };

        var task = new WorkflowTask("task2", "Task 2", "http", config, dependencies);

        task.Id.Should().Be("task2");
        task.Name.Should().Be("Task 2");
        task.AgentType.Should().Be("http");
        task.Config.Should().ContainKey("url");
        task.Dependencies.Should().Contain("task1");
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsDomainException()
    {
        var act = () => new WorkflowTask("", "Task", "http", new Dictionary<string, string>(), new List<string>());

        act.Should().Throw<DomainException>().WithMessage("Task ID cannot be empty");
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsDomainException()
    {
        var act = () => new WorkflowTask("task1", "", "http", new Dictionary<string, string>(), new List<string>());

        act.Should().Throw<DomainException>().WithMessage("Task name cannot be empty");
    }

    [Fact]
    public void Constructor_WithNameTooLong_ThrowsDomainException()
    {
        var act = () => new WorkflowTask("task1", new string('a', 101), "http", new Dictionary<string, string>(), new List<string>());

        act.Should().Throw<DomainException>().WithMessage("Task name cannot exceed 100 characters");
    }

    [Fact]
    public void Constructor_WithInvalidAgentType_ThrowsDomainException()
    {
        var act = () => new WorkflowTask("task1", "Task", "invalid", new Dictionary<string, string>(), new List<string>());

        act.Should().Throw<DomainException>().WithMessage("Invalid agent type: invalid");
    }

    [Theory]
    [InlineData("http")]
    [InlineData("python")]
    [InlineData("sql")]
    [InlineData("email")]
    public void Constructor_WithValidAgentTypes_CreatesWorkflowTask(string agentType)
    {
        var task = new WorkflowTask("task1", "Task", agentType, new Dictionary<string, string>(), new List<string>());

        task.AgentType.Should().Be(agentType);
    }

    [Fact]
    public void Constructor_WithNullConfig_UsesEmptyDictionary()
    {
        var task = new WorkflowTask("task1", "Task", "http", null!, new List<string>());

        task.Config.Should().NotBeNull();
        task.Config.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullDependencies_UsesEmptyList()
    {
        var task = new WorkflowTask("task1", "Task", "http", new Dictionary<string, string>(), null!);

        task.Dependencies.Should().NotBeNull();
        task.Dependencies.Should().BeEmpty();
    }
}
