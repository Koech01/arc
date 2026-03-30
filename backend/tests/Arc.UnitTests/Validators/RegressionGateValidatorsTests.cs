using FluentAssertions;
using Arc.Api.DTOs.RegressionGates;
namespace Arc.UnitTests.Validators;
using Arc.Api.Validators.RegressionGates;


public sealed class CreateRegressionGateRequestDtoValidatorTests
{
    private readonly CreateRegressionGateRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var request = new CreateRegressionGateRequestDto
        {
            Name = "Test Regression Gate",
            Description = "Test description",
            GoldenExecutionId = "exec-123",
            Rules = new List<DivergenceRuleDto>
            {
                new() { Type = "similarity_percentage", Threshold = 0.85 }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyName_ShouldFail(string name)
    {
        var request = new CreateRegressionGateRequestDto
        {
            Name = name,
            GoldenExecutionId = "exec-123",
            Rules = new List<DivergenceRuleDto>
            {
                new() { Type = "similarity_percentage", Threshold = 0.85 }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && 
                                            e.ErrorMessage == "Gate name is required");
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldFail()
    {
        var request = new CreateRegressionGateRequestDto
        {
            Name = new string('a', 201),
            GoldenExecutionId = "exec-123",
            Rules = new List<DivergenceRuleDto>
            {
                new() { Type = "similarity_percentage", Threshold = 0.85 }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && 
                                            e.ErrorMessage == "Gate name cannot exceed 200 characters");
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_ShouldFail()
    {
        var request = new CreateRegressionGateRequestDto
        {
            Name = "Test Gate",
            Description = new string('a', 1001),
            GoldenExecutionId = "exec-123",
            Rules = new List<DivergenceRuleDto>
            {
                new() { Type = "similarity_percentage", Threshold = 0.85 }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && 
                                            e.ErrorMessage == "Gate description cannot exceed 1000 characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyGoldenExecutionId_ShouldFail(string executionId)
    {
        var request = new CreateRegressionGateRequestDto
        {
            Name = "Test Gate",
            GoldenExecutionId = executionId,
            Rules = new List<DivergenceRuleDto>
            {
                new() { Type = "similarity_percentage", Threshold = 0.85 }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GoldenExecutionId" && 
                                            e.ErrorMessage == "Golden execution ID is required");
    }

    [Fact]
    public void Validate_WithEmptyRules_ShouldFail()
    {
        var request = new CreateRegressionGateRequestDto
        {
            Name = "Test Gate",
            GoldenExecutionId = "exec-123",
            Rules = new List<DivergenceRuleDto>()
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Rules" && 
                                            e.ErrorMessage == "At least one divergence rule is required");
    }

    [Fact]
    public void Validate_WithNullRules_ShouldFail()
    {
        var request = new CreateRegressionGateRequestDto
        {
            Name = "Test Gate",
            GoldenExecutionId = "exec-123",
            Rules = null!
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Rules");
    }
}

public sealed class DivergenceRuleDtoValidatorTests
{
    private readonly DivergenceRuleDtoValidator _validator = new();

    [Theory]
    [InlineData("similarity_percentage")]
    [InlineData("max_task_divergence")]
    [InlineData("critical_path_preservation")]
    [InlineData("no_status_degradation")]
    public void Validate_WithValidRuleTypes_ShouldPass(string ruleType)
    {
        var rule = new DivergenceRuleDto
        {
            Type = ruleType,
            Threshold = 0.85
        };

        var result = _validator.Validate(rule);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyType_ShouldFail(string type)
    {
        var rule = new DivergenceRuleDto
        {
            Type = type,
            Threshold = 0.85
        };

        var result = _validator.Validate(rule);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type" && 
                                            e.ErrorMessage == "Rule type is required");
    }

    [Fact]
    public void Validate_WithInvalidRuleType_ShouldFail()
    {
        var rule = new DivergenceRuleDto
        {
            Type = "invalid_rule_type",
            Threshold = 0.85
        };

        var result = _validator.Validate(rule);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type" && 
                                            e.ErrorMessage.Contains("similarity_percentage, max_task_divergence, critical_path_preservation, no_status_degradation"));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Validate_WithThresholdOutOfRange_ShouldFail(double threshold)
    {
        var rule = new DivergenceRuleDto
        {
            Type = "similarity_percentage",
            Threshold = threshold
        };

        var result = _validator.Validate(rule);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Threshold" && 
                                            e.ErrorMessage == "Threshold must be between 0.0 and 1.0");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Validate_WithValidThresholds_ShouldPass(double threshold)
    {
        var rule = new DivergenceRuleDto
        {
            Type = "similarity_percentage",
            Threshold = threshold
        };

        var result = _validator.Validate(rule);

        result.IsValid.Should().BeTrue();
    }
}

public sealed class RunGateTestRequestDtoValidatorTests
{
    private readonly RunGateTestRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidCandidateExecutionId_ShouldPass()
    {
        var request = new RunGateTestRequestDto
        {
            CandidateExecutionId = "exec-456"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyCandidateExecutionId_ShouldFail(string executionId)
    {
        var request = new RunGateTestRequestDto
        {
            CandidateExecutionId = executionId
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CandidateExecutionId" && 
                                            e.ErrorMessage == "Candidate execution ID is required");
    }
}

public sealed class MarkGoldenRequestDtoValidatorTests
{
    private readonly MarkGoldenRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidLabel_ShouldPass()
    {
        var request = new MarkGoldenRequestDto
        {
            Label = "Baseline v1.0"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullLabel_ShouldPass()
    {
        var request = new MarkGoldenRequestDto
        {
            Label = null
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithLabelTooLong_ShouldFail()
    {
        var request = new MarkGoldenRequestDto
        {
            Label = new string('a', 256)
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Label" && 
                                            e.ErrorMessage == "Label cannot exceed 255 characters");
    }
}