using FluentAssertions;
using Arc.Domain.Models;


namespace Arc.UnitTests.Domain;
public sealed class RegressionTestResultTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateResult()
    {
        // Arrange
        var gateId = RegressionGateId.NewId();
        var goldenId = new GoldenExecutionId("golden-exec-123");
        var ruleResult = new RuleEvaluationResult(
            ruleName: "Test Rule",
            passed: true,
            divergenceFound: false,
            message: "Test passed");
        var divergenceSummary = new DivergenceSummary(1, 0, 0);

        // Act
        var result = new RegressionTestResult(
            gateId,
            "Test Gate",
            "candidate-exec-123",
            goldenId,
            true,
            new[] { ruleResult },
            divergenceSummary);

        // Assert
        result.GateId.Should().Be(gateId);
        result.GateName.Should().Be("Test Gate");
        result.CandidateExecutionId.Should().Be("candidate-exec-123");
        result.GoldenExecutionId.Should().Be(goldenId);
        result.Passed.Should().BeTrue();
        result.RuleResults.Should().HaveCount(1);
        result.DivergenceSummary.Should().Be(divergenceSummary);
        result.TestedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidGateName_ShouldThrowArgumentException(string? invalidGateName)
    {
        // Arrange
        var gateId = RegressionGateId.NewId();
        var goldenId = new GoldenExecutionId("golden-exec-123");
        var ruleResult = new RuleEvaluationResult("Test", true, false, "Test");
        var divergenceSummary = new DivergenceSummary(1, 0, 0);

        // Act & Assert
        var act = () => new RegressionTestResult(
            gateId,
            invalidGateName!,
            "candidate-exec-123",
            goldenId,
            true,
            new[] { ruleResult },
            divergenceSummary);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Gate name*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCandidateExecutionId_ShouldThrowArgumentException(string? invalidExecutionId)
    {
        // Arrange
        var gateId = RegressionGateId.NewId();
        var goldenId = new GoldenExecutionId("golden-exec-123");
        var ruleResult = new RuleEvaluationResult("Test", true, false, "Test");
        var divergenceSummary = new DivergenceSummary(1, 0, 0);

        // Act & Assert
        var act = () => new RegressionTestResult(
            gateId,
            "Test Gate",
            invalidExecutionId!,
            goldenId,
            true,
            new[] { ruleResult },
            divergenceSummary);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Candidate execution ID*");
    }

    [Fact]
    public void Constructor_WithNoRuleResults_ShouldThrowArgumentException()
    {
        // Arrange
        var gateId = RegressionGateId.NewId();
        var goldenId = new GoldenExecutionId("golden-exec-123");
        var divergenceSummary = new DivergenceSummary(0, 0, 0);

        // Act & Assert
        var act = () => new RegressionTestResult(
            gateId,
            "Test Gate",
            "candidate-exec-123",
            goldenId,
            true,
            Array.Empty<RuleEvaluationResult>(),
            divergenceSummary);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one rule result*");
    }

    [Fact]
    public void Constructor_WithNullDivergenceSummary_ShouldThrowArgumentNullException()
    {
        // Arrange
        var gateId = RegressionGateId.NewId();
        var goldenId = new GoldenExecutionId("golden-exec-123");
        var ruleResult = new RuleEvaluationResult("Test", true, false, "Test");

        // Act & Assert
        var act = () => new RegressionTestResult(
            gateId,
            "Test Gate",
            "candidate-exec-123",
            goldenId,
            true,
            new[] { ruleResult },
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithCustomTestedAtUtc_ShouldUseProvidedValue()
    {
        // Arrange
        var gateId = RegressionGateId.NewId();
        var goldenId = new GoldenExecutionId("golden-exec-123");
        var ruleResult = new RuleEvaluationResult("Test", true, false, "Test");
        var divergenceSummary = new DivergenceSummary(1, 0, 0);
        var customTime = DateTime.UtcNow.AddHours(-2);

        // Act
        var result = new RegressionTestResult(
            gateId,
            "Test Gate",
            "candidate-exec-123",
            goldenId,
            true,
            new[] { ruleResult },
            divergenceSummary,
            customTime);

        // Assert
        result.TestedAtUtc.Should().Be(customTime);
    }

    [Fact]
    public void Constructor_WithFailedTest_ShouldSetPassedToFalse()
    {
        // Arrange
        var gateId = RegressionGateId.NewId();
        var goldenId = new GoldenExecutionId("golden-exec-123");
        var ruleResult = new RuleEvaluationResult("Test Rule", false, true, "Divergence found");
        var divergenceSummary = new DivergenceSummary(1, 1, 1);

        // Act
        var result = new RegressionTestResult(
            gateId,
            "Test Gate",
            "candidate-exec-123",
            goldenId,
            false,
            new[] { ruleResult },
            divergenceSummary);

        // Assert
        result.Passed.Should().BeFalse();
        result.RuleResults.First().Passed.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithMultipleRuleResults_ShouldStoreAllResults()
    {
        // Arrange
        var gateId = RegressionGateId.NewId();
        var goldenId = new GoldenExecutionId("golden-exec-123");
        var ruleResults = new[]
        {
            new RuleEvaluationResult("Rule 1", true, false, "Passed"),
            new RuleEvaluationResult("Rule 2", true, false, "Passed"),
            new RuleEvaluationResult("Rule 3", false, true, "Failed")
        };
        var divergenceSummary = new DivergenceSummary(3, 1, 1);

        // Act
        var result = new RegressionTestResult(
            gateId,
            "Test Gate",
            "candidate-exec-123",
            goldenId,
            false,
            ruleResults,
            divergenceSummary);

        // Assert
        result.RuleResults.Should().HaveCount(3);
        result.RuleResults.Should().ContainSingle(r => !r.Passed);
    }

    [Fact]
    public void Constructor_ShouldTrimGateName()
    {
        // Arrange
        var gateId = RegressionGateId.NewId();
        var goldenId = new GoldenExecutionId("golden-exec-123");
        var ruleResult = new RuleEvaluationResult("Test", true, false, "Test");
        var divergenceSummary = new DivergenceSummary(1, 0, 0);

        // Act
        var result = new RegressionTestResult(
            gateId,
            "  Test Gate  ",
            "candidate-exec-123",
            goldenId,
            true,
            new[] { ruleResult },
            divergenceSummary);

        // Assert
        result.GateName.Should().Be("Test Gate");
    }

    [Fact]
    public void Constructor_ShouldTrimCandidateExecutionId()
    {
        // Arrange
        var gateId = RegressionGateId.NewId();
        var goldenId = new GoldenExecutionId("golden-exec-123");
        var ruleResult = new RuleEvaluationResult("Test", true, false, "Test");
        var divergenceSummary = new DivergenceSummary(1, 0, 0);

        // Act
        var result = new RegressionTestResult(
            gateId,
            "Test Gate",
            "  candidate-exec-123  ",
            goldenId,
            true,
            new[] { ruleResult },
            divergenceSummary);

        // Assert
        result.CandidateExecutionId.Should().Be("candidate-exec-123");
    }
}