using FluentAssertions;
using Arc.Domain.Models;
using Arc.Domain.Exceptions;
namespace Arc.UnitTests.Domain;

public sealed class ExecutionGraphTests
{
    [Fact]
    public void Constructor_WithValidNodes_ShouldCreateGraph()
    {
        var task1 = new TaskNode("task-1", "Task 1");
        var task2 = new TaskNode("task-2", "Task 2", dependsOn: new[] { "task-1" });

        var graph = new ExecutionGraph(new[] { task1, task2 });

        graph.Nodes.Should().HaveCount(2);
        graph.Nodes.Should().Contain(n => n.Id == "task-1");
        graph.Nodes.Should().Contain(n => n.Id == "task-2");
    }

    [Fact]
    public void Constructor_WithNullNodes_ShouldThrowException()
    {
        var act = () => new ExecutionGraph(null!);

        act.Should().Throw<ExecutionGraphInvalidException>()
           .WithMessage("ExecutionGraph requires nodes.");
    }

    [Fact]
    public void Constructor_WithEmptyNodes_ShouldThrowException()
    {
        var act = () => new ExecutionGraph(Array.Empty<TaskNode>());

        act.Should().Throw<ExecutionGraphInvalidException>()
           .WithMessage("ExecutionGraph must contain at least one node.");
    }

    [Fact]
    public void Constructor_WithUnknownDependency_ShouldThrowException()
    {
        var task1 = new TaskNode("task-1", "Task 1");
        var task2 = new TaskNode("task-2", "Task 2", dependsOn: new[] { "task-99" });

        var act = () => new ExecutionGraph(new[] { task1, task2 });

        act.Should().Throw<ExecutionGraphInvalidException>()
           .WithMessage("*depends on unknown node*");
    }

    [Fact]
    public void Constructor_WithCyclicDependencies_ShouldThrowException()
    {
        var task1 = new TaskNode("task-1", "Task 1", dependsOn: new[] { "task-2" });
        var task2 = new TaskNode("task-2", "Task 2", dependsOn: new[] { "task-1" });

        var act = () => new ExecutionGraph(new[] { task1, task2 });

        act.Should().Throw<ExecutionGraphInvalidException>()
           .WithMessage("*contains a cycle*");
    }

    [Fact]
    public void Constructor_WithSelfCyclicDependency_ShouldThrowException()
    {
        var act = () => new TaskNode("task-1", "Task 1", dependsOn: new[] { "task-1" });

        act.Should().Throw<TaskNodeInvalidException>()
           .WithMessage("*cannot depend on itself*");
    }

    [Fact]
    public void Constructor_WithLinearDependencies_ShouldSucceed()
    {
        var task1 = new TaskNode("task-1", "Task 1");
        var task2 = new TaskNode("task-2", "Task 2", dependsOn: new[] { "task-1" });
        var task3 = new TaskNode("task-3", "Task 3", dependsOn: new[] { "task-2" });

        var graph = new ExecutionGraph(new[] { task1, task2, task3 });

        graph.Nodes.Should().HaveCount(3);
    }

    [Fact]
    public void Constructor_WithDAGStructure_ShouldSucceed()
    {
        var task1 = new TaskNode("task-1", "Task 1");
        var task2 = new TaskNode("task-2", "Task 2");
        var task3 = new TaskNode("task-3", "Task 3", dependsOn: new[] { "task-1", "task-2" });
        var task4 = new TaskNode("task-4", "Task 4", dependsOn: new[] { "task-3" });

        var graph = new ExecutionGraph(new[] { task1, task2, task3, task4 });

        graph.Nodes.Should().HaveCount(4);
    }
}

public sealed class TaskNodeTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateTaskNode()
    {
        var task = new TaskNode("task-1", "Task Name");

        task.Id.Should().Be("task-1");
        task.Name.Should().Be("Task Name");
        task.Prompt.Should().BeNull();
        task.LLMConfigId.Should().BeNull();
        task.DependsOn.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldCreateTaskNode()
    {
        var task = new TaskNode(
            "task-1",
            "Task Name",
            "Custom prompt",
            "llm-config-123",
            new[] { "task-0" });

        task.Id.Should().Be("task-1");
        task.Name.Should().Be("Task Name");
        task.Prompt.Should().Be("Custom prompt");
        task.LLMConfigId.Should().Be("llm-config-123");
        task.DependsOn.Should().ContainSingle().Which.Should().Be("task-0");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyId_ShouldThrowException(string id)
    {
        var act = () => new TaskNode(id, "Task Name");

        act.Should().Throw<TaskNodeInvalidException>()
           .WithMessage("*id cannot be null or empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyName_ShouldThrowException(string name)
    {
        var act = () => new TaskNode("task-1", name);

        act.Should().Throw<TaskNodeInvalidException>()
           .WithMessage("*name cannot be null or empty*");
    }

    [Fact]
    public void Constructor_WithPromptTooLong_ShouldThrowException()
    {
        var longPrompt = new string('a', 5001);

        var act = () => new TaskNode("task-1", "Task Name", longPrompt);

        act.Should().Throw<TaskNodeInvalidException>()
           .WithMessage("*prompt cannot exceed 5000 characters*");
    }

    [Fact]
    public void Constructor_WithMaxLengthPrompt_ShouldSucceed()
    {
        var maxPrompt = new string('a', 5000);

        var task = new TaskNode("task-1", "Task Name", maxPrompt);

        task.Prompt.Should().HaveLength(5000);
    }

    [Fact]
    public void Constructor_WithSelfDependency_ShouldThrowException()
    {
        var act = () => new TaskNode("task-1", "Task Name", dependsOn: new[] { "task-1" });

        act.Should().Throw<TaskNodeInvalidException>()
           .WithMessage("*cannot depend on itself*");
    }

    [Fact]
    public void Constructor_WithDuplicateDependencies_ShouldDeduplicateThem()
    {
        var task = new TaskNode("task-1", "Task Name", dependsOn: new[] { "task-0", "task-0", "task-0" });

        task.DependsOn.Should().ContainSingle().Which.Should().Be("task-0");
    }

    [Fact]
    public void Constructor_TrimsWhitespaceFromProperties()
    {
        var task = new TaskNode(
            "  task-1  ",
            "  Task Name  ",
            "  Custom prompt  ",
            "  llm-config-123  ");

        task.Id.Should().Be("task-1");
        task.Name.Should().Be("Task Name");
        task.Prompt.Should().Be("Custom prompt");
        task.LLMConfigId.Should().Be("llm-config-123");
    }
}