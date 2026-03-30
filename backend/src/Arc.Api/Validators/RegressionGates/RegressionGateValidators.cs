using FluentValidation;
using Arc.Api.DTOs.RegressionGates;
namespace Arc.Api.Validators.RegressionGates;


/// <summary>
/// Validator for CreateRegressionGateRequestDto.
/// </summary>
public sealed class CreateRegressionGateRequestDtoValidator : AbstractValidator<CreateRegressionGateRequestDto>
{
    public CreateRegressionGateRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Gate name is required")
            .MaximumLength(200).WithMessage("Gate name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Gate description cannot exceed 1000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.GoldenExecutionId)
            .NotEmpty().WithMessage("Golden execution ID is required");

        RuleFor(x => x.Rules)
            .NotNull().WithMessage("At least one divergence rule is required")
            .Must(rules => rules != null && rules.Count > 0).WithMessage("At least one divergence rule is required");

        RuleForEach(x => x.Rules).SetValidator(new DivergenceRuleDtoValidator()).When(x => x.Rules != null);
    }
}

/// <summary>
/// Validator for DivergenceRuleDto.
/// </summary>
public sealed class DivergenceRuleDtoValidator : AbstractValidator<DivergenceRuleDto>
{
    private static readonly string[] ValidRuleTypes = new[]
    {
        "similarity_percentage",
        "max_task_divergence",
        "critical_path_preservation",
        "no_status_degradation"
    };

    public DivergenceRuleDtoValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Rule type is required")
            .Must(type => !string.IsNullOrEmpty(type) && ValidRuleTypes.Contains(type.ToLowerInvariant()))
            .WithMessage($"Rule type must be one of: {string.Join(", ", ValidRuleTypes)}");

        RuleFor(x => x.Threshold)
            .InclusiveBetween(0.0, 1.0).WithMessage("Threshold must be between 0.0 and 1.0");
    }
}

/// <summary>
/// Validator for RunGateTestRequestDto.
/// </summary>
public sealed class RunGateTestRequestDtoValidator : AbstractValidator<RunGateTestRequestDto>
{
    public RunGateTestRequestDtoValidator()
    {
        RuleFor(x => x.CandidateExecutionId)
            .NotEmpty().WithMessage("Candidate execution ID is required");
    }
}

/// <summary>
/// Validator for ToggleRegressionGateRequestDto.
/// </summary>
public sealed class ToggleRegressionGateRequestDtoValidator : AbstractValidator<ToggleRegressionGateRequestDto>
{
    public ToggleRegressionGateRequestDtoValidator()
    {
        // IsActive is a boolean, no validation needed beyond type safety
    }
}

/// <summary>
/// Validator for MarkGoldenRequestDto.
/// </summary>
public sealed class MarkGoldenRequestDtoValidator : AbstractValidator<MarkGoldenRequestDto>
{
    public MarkGoldenRequestDtoValidator()
    {
        RuleFor(x => x.Label)
            .MaximumLength(255).WithMessage("Label cannot exceed 255 characters")
            .When(x => x.Label != null);
    }
}