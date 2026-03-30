using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;


namespace Arc.UnitTests.Execution;
public sealed class DeterministicTaskHasherTests
{
    [Fact]
    public void Compute_WithSameInputs_ShouldReturnSameHash()
    {
        // Arrange
        var node = new TaskNode("task-1", "Test Task", null, null, null);
        var dependencyResults = new List<TaskExecutionResult>
        {
            new TaskExecutionResult("dep-1", "Dependency 1", 1, TaskExecutionStatus.Succeeded, "output")
        };

        // Act
        var hash1 = DeterministicTaskHasher.Compute(node, dependencyResults);
        var hash2 = DeterministicTaskHasher.Compute(node, dependencyResults);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Compute_WithDifferentTaskIds_ShouldReturnDifferentHashes()
    {
        // Arrange
        var node1 = new TaskNode("task-1", "Test Task", null, null, null);
        var node2 = new TaskNode("task-2", "Test Task", null, null, null);
        var dependencyResults = new List<TaskExecutionResult>();

        // Act
        var hash1 = DeterministicTaskHasher.Compute(node1, dependencyResults);
        var hash2 = DeterministicTaskHasher.Compute(node2, dependencyResults);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Compute_WithDifferentTaskNames_ShouldReturnDifferentHashes()
    {
        // Arrange
        var node1 = new TaskNode("task-1", "Test Task 1", null, null, null);
        var node2 = new TaskNode("task-1", "Test Task 2", null, null, null);
        var dependencyResults = new List<TaskExecutionResult>();

        // Act
        var hash1 = DeterministicTaskHasher.Compute(node1, dependencyResults);
        var hash2 = DeterministicTaskHasher.Compute(node2, dependencyResults);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Compute_WithDifferentDependencyResults_ShouldReturnDifferentHashes()
    {
        // Arrange
        var node = new TaskNode("task-1", "Test Task", null, null, null);
        var dependencyResults1 = new List<TaskExecutionResult>
        {
            new TaskExecutionResult("dep-1", "Dependency 1", 1, TaskExecutionStatus.Succeeded, "output1")
        };
        var dependencyResults2 = new List<TaskExecutionResult>
        {
            new TaskExecutionResult("dep-1", "Dependency 1", 1, TaskExecutionStatus.Failed, "output2")
        };

        // Act
        var hash1 = DeterministicTaskHasher.Compute(node, dependencyResults1);
        var hash2 = DeterministicTaskHasher.Compute(node, dependencyResults2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Compute_WithNoDependencies_ShouldReturnValidHash()
    {
        // Arrange
        var node = new TaskNode("task-1", "Test Task", null, null, null);
        var dependencyResults = new List<TaskExecutionResult>();

        // Act
        var hash = DeterministicTaskHasher.Compute(node, dependencyResults);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64); // SHA256 hex string length
        hash.Should().MatchRegex("^[a-f0-9]{64}$"); // Lowercase hex
    }

    [Fact]
    public void Compute_WithMultipleDependencies_ShouldIncludeAllDependencies()
    {
        // Arrange
        var node = new TaskNode("task-1", "Test Task", null, null, null);
        var dependencyResults1 = new List<TaskExecutionResult>
        {
            new TaskExecutionResult("dep-1", "Dep 1", 1, TaskExecutionStatus.Succeeded, "output1"),
            new TaskExecutionResult("dep-2", "Dep 2", 2, TaskExecutionStatus.Succeeded, "output2")
        };
        var dependencyResults2 = new List<TaskExecutionResult>
        {
            new TaskExecutionResult("dep-1", "Dep 1", 1, TaskExecutionStatus.Succeeded, "output1")
        };

        // Act
        var hash1 = DeterministicTaskHasher.Compute(node, dependencyResults1);
        var hash2 = DeterministicTaskHasher.Compute(node, dependencyResults2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Compute_WithDependenciesInDifferentOrder_ShouldReturnSameHashDueToOrdering()
    {
        // Arrange
        var node = new TaskNode("task-1", "Test Task", null, null, null);
        var dependencyResults1 = new List<TaskExecutionResult>
        {
            new TaskExecutionResult("dep-a", "Dep A", 1, TaskExecutionStatus.Succeeded, "output"),
            new TaskExecutionResult("dep-b", "Dep B", 2, TaskExecutionStatus.Succeeded, "output")
        };
        var dependencyResults2 = new List<TaskExecutionResult>
        {
            new TaskExecutionResult("dep-b", "Dep B", 2, TaskExecutionStatus.Succeeded, "output"),
            new TaskExecutionResult("dep-a", "Dep A", 1, TaskExecutionStatus.Succeeded, "output")
        };

        // Act
        var hash1 = DeterministicTaskHasher.Compute(node, dependencyResults1);
        var hash2 = DeterministicTaskHasher.Compute(node, dependencyResults2);

        // Assert
        hash1.Should().Be(hash2); // Should be same due to OrderBy in implementation
    }

    [Fact]
    public void Compute_ShouldReturnLowercaseHexString()
    {
        // Arrange
        var node = new TaskNode("task-1", "Test Task", null, null, null);
        var dependencyResults = new List<TaskExecutionResult>();

        // Act
        var hash = DeterministicTaskHasher.Compute(node, dependencyResults);

        // Assert
        hash.Should().MatchRegex("^[a-f0-9]+$");
        hash.Should().NotContain("[A-F]");
    }

    [Fact]
    public void Compute_WithComplexScenario_ShouldProduceDeterministicHash()
    {
        // Arrange
        var node = new TaskNode(
            "complex-task",
            "Complex Task with Long Name",
            "Custom prompt",
            "llm-config-123",
            new[] { "dep-1", "dep-2", "dep-3" });

        var dependencyResults = new List<TaskExecutionResult>
        {
            new TaskExecutionResult("dep-1", "First Dependency", 1, TaskExecutionStatus.Succeeded, "output1"),
            new TaskExecutionResult("dep-2", "Second Dependency", 2, TaskExecutionStatus.Succeeded, "output2"),
            new TaskExecutionResult("dep-3", "Third Dependency", 3, TaskExecutionStatus.Succeeded, "output3")
        };

        // Act
        var hash1 = DeterministicTaskHasher.Compute(node, dependencyResults);
        var hash2 = DeterministicTaskHasher.Compute(node, dependencyResults);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64);
    }
}