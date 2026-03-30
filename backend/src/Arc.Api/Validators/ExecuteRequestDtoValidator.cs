using Arc.Api.DTOs;
using FluentValidation;
namespace Arc.Api.Validators;


public sealed class ExecuteRequestDtoValidator : AbstractValidator<ExecuteRequestDto>
{
    public ExecuteRequestDtoValidator()
    {
        RuleFor(x => x.Input)
            .NotEmpty()
            .WithMessage("Input is required.");
    }
}