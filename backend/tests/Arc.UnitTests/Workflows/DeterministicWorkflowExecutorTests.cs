using NSubstitute;
using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;
namespace Arc.UnitTests.Workflows;
using Arc.Infrastructure.Workflows;


public sealed class DeterministicWorkflowExecutorTests
{
    [Fact]
    public void Execute_WithValidWorkflow_ReturnsExecutionResult()
    {
        var userId = new UserId(Guid.NewGuid());
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string>()),
            new("task2", "Task 2", "python", new Dictionary<string, string>(), new List<string> { "task1" })
        };
        var workflow = new Workflow(
            "wf1",
            "Test Workflow",
            "Test Description",
            tasks,
            "manual",
            userId,
            DateTime.UtcNow);

        var mockEngine = Substitute.For<IExecutionEngine>();
        var expectedResult = new ExecutionResult(
            userId,
            new List<TaskExecutionResult>
            {
                new("task1", "Task 1", 0, TaskExecutionStatus.Succeeded, "output1"),
                new("task2", "Task 2", 1, TaskExecutionStatus.Succeeded, "output2")
            });
        mockEngine.Execute(Arg.Any<ExecutionGraph>()).Returns(expectedResult);

        var executor = new DeterministicWorkflowExecutor(mockEngine);

        var result = executor.Execute(workflow);

        Assert.NotNull(result);
        Assert.Equal(2, result.Tasks.Count);
        mockEngine.Received(1).Execute(Arg.Is<ExecutionGraph>(g => g.Nodes.Count == 2));
    }

    [Fact]
    public void Execute_WithNullWorkflow_ThrowsArgumentNullException()
    {
        var mockEngine = Substitute.For<IExecutionEngine>();
        var executor = new DeterministicWorkflowExecutor(mockEngine);

        Assert.Throws<ArgumentNullException>(() => executor.Execute(null!));
    }

    [Fact]
    public void Execute_ConvertsWorkflowTasksToTaskNodes()
    {
        var userId = new UserId(Guid.NewGuid());
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string>()),
            new("task2", "Task 2", "sql", new Dictionary<string, string>(), new List<string> { "task1" }),
            new("task3", "Task 3", "email", new Dictionary<string, string>(), new List<string> { "task1", "task2" })
        };
        var workflow = new Workflow(
            "wf1",
            "Complex Workflow",
            "Test",
            tasks,
            "manual",
            userId,
            DateTime.UtcNow);

        ExecutionGraph? capturedGraph = null;
        var mockEngine = Substitute.For<IExecutionEngine>();
        mockEngine.Execute(Arg.Do<ExecutionGraph>(g => capturedGraph = g))
            .Returns(new ExecutionResult(userId, new List<TaskExecutionResult>
            {
                new("task1", "Task 1", 0, TaskExecutionStatus.Succeeded, "output1")
            }));

        var executor = new DeterministicWorkflowExecutor(mockEngine);
        executor.Execute(workflow);

        Assert.NotNull(capturedGraph);
        Assert.Equal(3, capturedGraph.Nodes.Count);

        var task1 = capturedGraph.Nodes.First(n => n.Id == "task1");
        var task2 = capturedGraph.Nodes.First(n => n.Id == "task2");
        var task3 = capturedGraph.Nodes.First(n => n.Id == "task3");

        Assert.Equal("Task 1", task1.Name);
        Assert.Empty(task1.DependsOn);

        Assert.Equal("Task 2", task2.Name);
        Assert.Single(task2.DependsOn);
        Assert.Contains("task1", task2.DependsOn);

        Assert.Equal("Task 3", task3.Name);
        Assert.Equal(2, task3.DependsOn.Count);
        Assert.Contains("task1", task3.DependsOn);
        Assert.Contains("task2", task3.DependsOn);
    }

    [Fact]
    public void Execute_PreservesDependencyStructure()
    {
        var userId = new UserId(Guid.NewGuid());
        var tasks = new List<WorkflowTask>
        {
            new("A", "Task A", "http", new Dictionary<string, string>(), new List<string>()),
            new("B", "Task B", "python", new Dictionary<string, string>(), new List<string> { "A" }),
            new("C", "Task C", "sql", new Dictionary<string, string>(), new List<string> { "A" }),
            new("D", "Task D", "email", new Dictionary<string, string>(), new List<string> { "B", "C" })
        };
        var workflow = new Workflow(
            "wf1",
            "Diamond Workflow",
            "Test",
            tasks,
            "manual",
            userId,
            DateTime.UtcNow);

        ExecutionGraph? capturedGraph = null;
        var mockEngine = Substitute.For<IExecutionEngine>();
        mockEngine.Execute(Arg.Do<ExecutionGraph>(g => capturedGraph = g))
            .Returns(new ExecutionResult(userId, new List<TaskExecutionResult>
            {
                new("A", "Task A", 0, TaskExecutionStatus.Succeeded, "output")
            }));

        var executor = new DeterministicWorkflowExecutor(mockEngine);
        executor.Execute(workflow);

        Assert.NotNull(capturedGraph);
        var nodeD = capturedGraph.Nodes.First(n => n.Id == "D");
        Assert.Equal(2, nodeD.DependsOn.Count);
        Assert.Contains("B", nodeD.DependsOn);
        Assert.Contains("C", nodeD.DependsOn);
    }
}