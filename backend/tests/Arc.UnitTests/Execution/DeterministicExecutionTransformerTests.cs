using NSubstitute;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;
namespace Arc.UnitTests.Execution;
using Arc.Infrastructure.Execution;


public sealed class DeterministicExecutionTransformerTests
{
    private readonly IExecutionResultStore _mockExecutionResultStore;
    private readonly DeterministicExecutionTransformer _transformer;

    public DeterministicExecutionTransformerTests()
    {
        _mockExecutionResultStore = Substitute.For<IExecutionResultStore>();
        _transformer = new DeterministicExecutionTransformer(_mockExecutionResultStore);
    }

    [Fact]
    public async Task TransformAsync_WithValidInput_ReturnsTransformedExecution()
    {
        // Arrange
        var originalExecutionId = "original-exec-123";
        var originalExecution = CreateSampleExecution();
        _mockExecutionResultStore.GetAsync(originalExecutionId).Returns(originalExecution);

        var transformationRules = new ExecutionTransformationRules(
            TaskMappings: new[]
            {
                new TaskMappingRule("task-1", "transformed-task-1", "Transformed Task 1")
            },
            DependencyRewiring: Array.Empty<DependencyRewiringRule>()
        );

        // Act
        var result = await _transformer.TransformAsync(originalExecutionId, transformationRules);

        // Assert
        result.Should().NotBeNull();
        result.TransformedExecutionId.Should().StartWith("transformed-");
        result.TransformedExecution.Tasks.Should().HaveCount(3);
        result.TransformedExecution.Tasks.First(t => t.TaskId == "transformed-task-1").TaskName
            .Should().Be("Transformed Task 1");
    }

    [Fact]
    public async Task TransformAsync_WithSameInputs_GeneratesSameTransformedExecutionId()
    {
        // Arrange
        var originalExecutionId = "original-exec-123";
        var originalExecution = CreateSampleExecution();
        _mockExecutionResultStore.GetAsync(originalExecutionId).Returns(originalExecution);

        var transformationRules = new ExecutionTransformationRules(
            TaskMappings: new[]
            {
                new TaskMappingRule("task-1", "transformed-task-1")
            },
            DependencyRewiring: Array.Empty<DependencyRewiringRule>()
        );

        // Act
        var result1 = await _transformer.TransformAsync(originalExecutionId, transformationRules);
        var result2 = await _transformer.TransformAsync(originalExecutionId, transformationRules);

        // Assert
        result1.TransformedExecutionId.Should().Be(result2.TransformedExecutionId);
    }

    [Fact]
    public async Task TransformAsync_WithDependencyRewiring_AppliesNewDependencies()
    {
        // Arrange
        var originalExecutionId = "original-exec-123";
        var originalExecution = CreateSampleExecution();
        _mockExecutionResultStore.GetAsync(originalExecutionId).Returns(originalExecution);

        var transformationRules = new ExecutionTransformationRules(
            TaskMappings: Array.Empty<TaskMappingRule>(),
            DependencyRewiring: new[]
            {
                new DependencyRewiringRule("task-2", new[] { "task-3" })
            }
        );

        // Act
        var result = await _transformer.TransformAsync(originalExecutionId, transformationRules);

        // Assert
        result.TransformedGraph.Nodes.First(n => n.Id == "task-2").DependsOn
            .Should().BeEquivalentTo(new[] { "task-3" });
    }

    [Fact]
    public async Task TransformAsync_WithNonExistentExecution_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentExecutionId = "non-existent-123";
        _mockExecutionResultStore.GetAsync(nonExistentExecutionId).Returns((ExecutionResult?)null);

        var transformationRules = new ExecutionTransformationRules(
            Array.Empty<TaskMappingRule>(),
            Array.Empty<DependencyRewiringRule>()
        );

        // Act & Assert
        await _transformer.Invoking(t => t.TransformAsync(nonExistentExecutionId, transformationRules))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Execution with ID 'non-existent-123' not found.");
    }

    [Fact]
    public async Task TransformAsync_WithNullExecutionId_ThrowsArgumentException()
    {
        // Arrange
        var transformationRules = new ExecutionTransformationRules(
            Array.Empty<TaskMappingRule>(),
            Array.Empty<DependencyRewiringRule>()
        );

        // Act & Assert
        await _transformer.Invoking(t => t.TransformAsync(null!, transformationRules))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("ExecutionId cannot be null or empty. (Parameter 'executionId')");
    }

    [Fact]
    public async Task TransformAsync_WithNullTransformationRules_ThrowsArgumentNullException()
    {
        // Act & Assert
        await _transformer.Invoking(t => t.TransformAsync("exec-123", null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'transformationRules')");
    }

    [Fact]
    public async Task TransformAsync_StoresTransformedExecution()
    {
        // Arrange
        var originalExecutionId = "original-exec-123";
        var originalExecution = CreateSampleExecution();
        _mockExecutionResultStore.GetAsync(originalExecutionId).Returns(originalExecution);

        var transformationRules = new ExecutionTransformationRules(
            Array.Empty<TaskMappingRule>(),
            Array.Empty<DependencyRewiringRule>()
        );

        // Act
        var result = await _transformer.TransformAsync(originalExecutionId, transformationRules);

        // Assert
        await _mockExecutionResultStore.Received(1)
            .StoreAsync(result.TransformedExecutionId, result.TransformedExecution);
    }

    [Fact]
    public async Task TransformAsync_PreservesExecutionOrderInTransformedTasks()
    {
        // Arrange
        var originalExecutionId = "original-exec-123";
        var originalExecution = CreateSampleExecution();
        _mockExecutionResultStore.GetAsync(originalExecutionId).Returns(originalExecution);

        var transformationRules = new ExecutionTransformationRules(
            Array.Empty<TaskMappingRule>(),
            Array.Empty<DependencyRewiringRule>()
        );

        // Act
        var result = await _transformer.TransformAsync(originalExecutionId, transformationRules);

        // Assert
        var orderedTasks = result.TransformedExecution.Tasks.OrderBy(t => t.ExecutionOrder).ToArray();
        for (int i = 0; i < orderedTasks.Length; i++)
        {
            orderedTasks[i].ExecutionOrder.Should().Be(i + 1);
        }
    }

    [Fact]
    public async Task TransformAsync_WithComplexTransformation_AppliesAllRulesCorrectly()
    {
        // Arrange
        var originalExecutionId = "original-exec-123";
        var originalExecution = CreateSampleExecution();
        _mockExecutionResultStore.GetAsync(originalExecutionId).Returns(originalExecution);

        var transformationRules = new ExecutionTransformationRules(
            TaskMappings: new[]
            {
                new TaskMappingRule("task-1", "new-task-1", "New Task 1"),
                new TaskMappingRule("task-3", "new-task-3", "New Task 3")
            },
            DependencyRewiring: new[]
            {
                new DependencyRewiringRule("task-2", new[] { "new-task-3" })
            }
        );

        // Act
        var result = await _transformer.TransformAsync(originalExecutionId, transformationRules);

        // Assert
        result.TransformedExecution.Tasks.Should().HaveCount(3);
        
        var task1 = result.TransformedExecution.Tasks.First(t => t.TaskId == "new-task-1");
        task1.TaskName.Should().Be("New Task 1");
        
        var task3 = result.TransformedExecution.Tasks.First(t => t.TaskId == "new-task-3");
        task3.TaskName.Should().Be("New Task 3");
        
        var task2Node = result.TransformedGraph.Nodes.First(n => n.Id == "task-2");
        task2Node.DependsOn.Should().BeEquivalentTo(new[] { "new-task-3" });
    }

    private static ExecutionResult CreateSampleExecution()
    {
        var tasks = new[]
        {
            new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "Output 1"),
            new TaskExecutionResult("task-2", "Task 2", 2, TaskExecutionStatus.Succeeded, "Output 2"),
            new TaskExecutionResult("task-3", "Task 3", 3, TaskExecutionStatus.Succeeded, "Output 3")
        };

        return new ExecutionResult(UserId.Anonymous, tasks);
    }
}