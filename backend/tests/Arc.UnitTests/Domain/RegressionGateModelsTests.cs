using FluentAssertions;
using Arc.Domain.Models;
namespace Arc.UnitTests.Domain;


public sealed class RuleEvaluationResultTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateResult()
    {
        // Act
        var result = new RuleEvaluationResult(
            ruleName: "Test Rule",
            passed: true,
            divergenceFound: false,
            message: "Test passed successfully");

        // Assert
        result.RuleName.Should().Be("Test Rule");
        result.Passed.Should().BeTrue();
        result.DivergenceFound.Should().BeFalse();
        result.Message.Should().Be("Test passed successfully");
    }

    [Fact]
    public void Constructor_WithFailedRule_ShouldSetPassedToFalse()
    {
        // Act
        var result = new RuleEvaluationResult(
            ruleName: "Failed Rule",
            passed: false,
            divergenceFound: true,
            message: "Divergence detected");

        // Assert
        result.Passed.Should().BeFalse();
        result.DivergenceFound.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithPassedRuleButDivergenceFound_ShouldAllowCreation()
    {
        // Act
        var result = new RuleEvaluationResult(
            ruleName: "Warning Rule",
            passed: true,
            divergenceFound: true,
            message: "Non-critical divergence");

        // Assert
        result.Passed.Should().BeTrue();
        result.DivergenceFound.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldCreateResult()
    {
        // Act
        var result = new RuleEvaluationResult(
            ruleName: "Test Rule",
            passed: true,
            divergenceFound: false,
            message: string.Empty);

        // Assert
        result.Message.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullMessage_ShouldCreateResultWithNullMessage()
    {
        // Act
        var result = new RuleEvaluationResult(
            ruleName: "Test Rule",
            passed: true,
            divergenceFound: false,
            message: null);

        // Assert
        result.Message.Should().BeNull();
    }
}

public sealed class DivergenceSummaryTests
{
    [Fact]
    public void Constructor_WithValidCounts_ShouldCreateSummary()
    {
        // Act
        var summary = new DivergenceSummary(
            totalRulesEvaluated: 10,
            rulesFailed: 2,
            divergencesDetected: 3);

        // Assert
        summary.TotalRulesEvaluated.Should().Be(10);
        summary.RulesFailed.Should().Be(2);
        summary.DivergencesDetected.Should().Be(3);
    }

    [Fact]
    public void Constructor_WithZeroCounts_ShouldCreateSummary()
    {
        // Act
        var summary = new DivergenceSummary(0, 0, 0);

        // Assert
        summary.TotalRulesEvaluated.Should().Be(0);
        summary.RulesFailed.Should().Be(0);
        summary.DivergencesDetected.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithAllRulesFailed_ShouldCreateSummary()
    {
        // Act
        var summary = new DivergenceSummary(5, 5, 5);

        // Assert
        summary.TotalRulesEvaluated.Should().Be(5);
        summary.RulesFailed.Should().Be(5);
        summary.DivergencesDetected.Should().Be(5);
    }

    [Fact]
    public void Constructor_WithNoDivergences_ShouldCreateSummary()
    {
        // Act
        var summary = new DivergenceSummary(10, 0, 0);

        // Assert
        summary.TotalRulesEvaluated.Should().Be(10);
        summary.RulesFailed.Should().Be(0);
        summary.DivergencesDetected.Should().Be(0);
    }
}

public sealed class GoldenExecutionMetadataTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateMetadata()
    {
        // Arrange
        var executionId = "execution-123";
        var markedByUserId = UserId.From(Guid.NewGuid());
        var markedAt = DateTime.UtcNow;

        // Act
        var metadata = new GoldenExecutionMetadata(
            executionId,
            markedByUserId,
            markedAt,
            "Baseline for v1.0");

        // Assert
        metadata.ExecutionId.Should().Be(executionId);
        metadata.MarkedByUserId.Should().Be(markedByUserId);
        metadata.MarkedAt.Should().Be(markedAt);
        metadata.Notes.Should().Be("Baseline for v1.0");
    }

    [Fact]
    public void Constructor_WithNullNotes_ShouldCreateMetadata()
    {
        // Arrange
        var executionId = "execution-123";
        var markedByUserId = UserId.From(Guid.NewGuid());
        var markedAt = DateTime.UtcNow;

        // Act
        var metadata = new GoldenExecutionMetadata(
            executionId,
            markedByUserId,
            markedAt,
            null);

        // Assert
        metadata.Notes.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyNotes_ShouldCreateMetadata()
    {
        // Arrange
        var executionId = "execution-123";
        var markedByUserId = UserId.From(Guid.NewGuid());
        var markedAt = DateTime.UtcNow;

        // Act
        var metadata = new GoldenExecutionMetadata(
            executionId,
            markedByUserId,
            markedAt,
            string.Empty);

        // Assert
        metadata.Notes.Should().BeEmpty();
    }
}