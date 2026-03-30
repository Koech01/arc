using FluentAssertions;
using Arc.Domain.Models;
using Arc.Domain.Exceptions;
namespace Arc.UnitTests.Domain;


public sealed class RegressionGateTests
{
    private readonly RegressionGateId _testGateId = new(Guid.NewGuid());
    private readonly UserId _testUserId = new(Guid.NewGuid());
    private readonly GoldenExecutionId _testGoldenExecId = new("golden-exec-1");

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateRegressionGate()
    {
        var rules = new[] { new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 0.85) };

        var gate = new RegressionGate(
            _testGateId,
            _testUserId,
            "Test Gate",
            _testGoldenExecId,
            rules);

        gate.Id.Should().Be(_testGateId);
        gate.OwnerId.Should().Be(_testUserId);
        gate.Name.Should().Be("Test Gate");
        gate.GoldenExecutionId.Should().Be(_testGoldenExecId);
        gate.Rules.Should().HaveCount(1);
        gate.IsActive.Should().BeTrue();
        gate.Description.Should().BeNull();
        gate.WorkflowId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldCreateRegressionGate()
    {
        var rules = new[] { new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 0.85) };
        var createdAt = DateTime.UtcNow;

        var gate = new RegressionGate(
            _testGateId,
            _testUserId,
            "Test Gate",
            _testGoldenExecId,
            rules,
            "Test description",
            "workflow-123",
            false,
            createdAt);

        gate.Id.Should().Be(_testGateId);
        gate.OwnerId.Should().Be(_testUserId);
        gate.Name.Should().Be("Test Gate");
        gate.GoldenExecutionId.Should().Be(_testGoldenExecId);
        gate.Rules.Should().HaveCount(1);
        gate.IsActive.Should().BeFalse();
        gate.Description.Should().Be("Test description");
        gate.WorkflowId.Should().Be("workflow-123");
        gate.CreatedAtUtc.Should().Be(createdAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyName_ShouldThrowException(string name)
    {
        var rules = new[] { new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 0.85) };

        var act = () => new RegressionGate(
            _testGateId,
            _testUserId,
            name,
            _testGoldenExecId,
            rules);

        act.Should().Throw<RegressionGateInvalidException>()
           .WithMessage("*name cannot be null or empty*");
    }

    [Fact]
    public void Constructor_WithNameTooLong_ShouldThrowException()
    {
        var rules = new[] { new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 0.85) };

        var act = () => new RegressionGate(
            _testGateId,
            _testUserId,
            new string('a', 201),
            _testGoldenExecId,
            rules);

        act.Should().Throw<RegressionGateInvalidException>()
           .WithMessage("*name cannot exceed 200 characters*");
    }

    [Fact]
    public void Constructor_WithDescriptionTooLong_ShouldThrowException()
    {
        var rules = new[] { new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 0.85) };

        var act = () => new RegressionGate(
            _testGateId,
            _testUserId,
            "Test Gate",
            _testGoldenExecId,
            rules,
            new string('a', 1001));

        act.Should().Throw<RegressionGateInvalidException>()
           .WithMessage("*description cannot exceed 1000 characters*");
    }

    [Fact]
    public void Constructor_WithEmptyRules_ShouldThrowException()
    {
        var act = () => new RegressionGate(
            _testGateId,
            _testUserId,
            "Test Gate",
            _testGoldenExecId,
            Array.Empty<DivergenceRule>());

        act.Should().Throw<RegressionGateInvalidException>()
           .WithMessage("*must have at least one divergence rule*");
    }

    [Fact]
    public void Constructor_WithNullRules_ShouldThrowException()
    {
        var act = () => new RegressionGate(
            _testGateId,
            _testUserId,
            "Test Gate",
            _testGoldenExecId,
            null!);

        act.Should().Throw<RegressionGateInvalidException>()
           .WithMessage("*must have at least one divergence rule*");
    }

    [Fact]
    public void Constructor_TrimsNameAndDescription()
    {
        var rules = new[] { new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 0.85) };

        var gate = new RegressionGate(
            _testGateId,
            _testUserId,
            "  Test Gate  ",
            _testGoldenExecId,
            rules,
            "  Test description  ");

        gate.Name.Should().Be("Test Gate");
        gate.Description.Should().Be("Test description");
    }

    [Fact]
    public void Constructor_WithMultipleRules_ShouldStoreAllRules()
    {
        var rules = new[]
        {
            new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 0.85),
            new DivergenceRule(DivergenceRuleType.MaxTaskDivergence, 0.10),
            new DivergenceRule(DivergenceRuleType.CriticalPathPreservation, 1.0)
        };

        var gate = new RegressionGate(
            _testGateId,
            _testUserId,
            "Test Gate",
            _testGoldenExecId,
            rules);

        gate.Rules.Should().HaveCount(3);
        gate.Rules[0].Type.Should().Be(DivergenceRuleType.SimilarityPercentage);
        gate.Rules[1].Type.Should().Be(DivergenceRuleType.MaxTaskDivergence);
        gate.Rules[2].Type.Should().Be(DivergenceRuleType.CriticalPathPreservation);
    }

    [Fact]
    public void WithIsActive_ShouldReturnNewGateWithUpdatedStatus()
    {
        var rules = new[] { new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 0.85) };
        var gate = new RegressionGate(
            _testGateId,
            _testUserId,
            "Test Gate",
            _testGoldenExecId,
            rules,
            isActive: true);

        var updatedGate = gate.WithIsActive(false);

        updatedGate.IsActive.Should().BeFalse();
        updatedGate.Id.Should().Be(gate.Id);
        updatedGate.Name.Should().Be(gate.Name);
        updatedGate.OwnerId.Should().Be(gate.OwnerId);
        updatedGate.GoldenExecutionId.Should().Be(gate.GoldenExecutionId);
    }
}

public sealed class DivergenceRuleTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(0.85)]
    [InlineData(1.0)]
    public void Constructor_WithValidThreshold_ShouldCreateRule(double threshold)
    {
        var rule = new DivergenceRule(DivergenceRuleType.SimilarityPercentage, threshold);

        rule.Type.Should().Be(DivergenceRuleType.SimilarityPercentage);
        rule.Threshold.Should().Be(threshold);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-1.0)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Constructor_WithInvalidThreshold_ShouldThrowException(double threshold)
    {
        var act = () => new DivergenceRule(DivergenceRuleType.SimilarityPercentage, threshold);

        act.Should().Throw<DivergenceRuleInvalidException>()
           .WithMessage($"*Threshold must be between 0.0 and 1.0, got {threshold}*");
    }

    [Fact]
    public void Constructor_WithAllRuleTypes_ShouldSucceed()
    {
        var rule1 = new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 0.85);
        var rule2 = new DivergenceRule(DivergenceRuleType.MaxTaskDivergence, 0.10);
        var rule3 = new DivergenceRule(DivergenceRuleType.CriticalPathPreservation, 1.0);
        var rule4 = new DivergenceRule(DivergenceRuleType.NoStatusDegradation, 1.0);

        rule1.Type.Should().Be(DivergenceRuleType.SimilarityPercentage);
        rule2.Type.Should().Be(DivergenceRuleType.MaxTaskDivergence);
        rule3.Type.Should().Be(DivergenceRuleType.CriticalPathPreservation);
        rule4.Type.Should().Be(DivergenceRuleType.NoStatusDegradation);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var rule = new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 0.85);

        var result = rule.ToString();

        result.Should().Contain("85%");
    }
}

public sealed class RegressionGateIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_ShouldCreateId()
    {
        var guid = Guid.NewGuid();

        var id = new RegressionGateId(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void TwoIds_WithSameGuid_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = new RegressionGateId(guid);
        var id2 = new RegressionGateId(guid);

        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void TwoIds_WithDifferentGuids_ShouldNotBeEqual()
    {
        var id1 = new RegressionGateId(Guid.NewGuid());
        var id2 = new RegressionGateId(Guid.NewGuid());

        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }
}

public sealed class GoldenExecutionIdTests
{
    [Fact]
    public void Constructor_WithValidValue_ShouldCreateId()
    {
        var id = new GoldenExecutionId("golden-exec-123");

        id.Value.Should().Be("golden-exec-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyValue_ShouldThrowException(string value)
    {
        var act = () => new GoldenExecutionId(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TwoIds_WithSameValue_ShouldBeEqual()
    {
        var id1 = new GoldenExecutionId("golden-exec-123");
        var id2 = new GoldenExecutionId("golden-exec-123");

        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void TwoIds_WithDifferentValues_ShouldNotBeEqual()
    {
        var id1 = new GoldenExecutionId("golden-exec-123");
        var id2 = new GoldenExecutionId("golden-exec-456");

        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }
}