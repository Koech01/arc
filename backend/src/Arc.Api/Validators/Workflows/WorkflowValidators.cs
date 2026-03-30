using FluentValidation;
using Arc.Api.DTOs.Workflows;
namespace Arc.Api.Validators.Workflows;


public sealed class CreateWorkflowRequestDtoValidator : AbstractValidator<CreateWorkflowRequestDto>
{
    public CreateWorkflowRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workflow name is required")
            .Length(1, 200).WithMessage("Workflow name must be between 1 and 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Workflow description cannot exceed 1000 characters");

        RuleFor(x => x.Tasks)
            .NotEmpty().WithMessage("At least one task is required")
            .Must(tasks => tasks != null && tasks.Count > 0).WithMessage("At least one task is required");

        RuleForEach(x => x.Tasks).SetValidator(new WorkflowTaskDtoValidator());

        RuleFor(x => x.TriggerType)
            .NotEmpty().WithMessage("Trigger type is required")
            .Must(t => t is "manual" or "scheduled" or "webhook")
            .WithMessage("Trigger type must be 'manual', 'scheduled', or 'webhook'");

        RuleFor(x => x.LLMConfigId)
            .MaximumLength(16).WithMessage("LLM config ID cannot exceed 16 characters")
            .When(x => x.LLMConfigId != null);
    }
}

public sealed class WorkflowTaskDtoValidator : AbstractValidator<WorkflowTaskDto>
{
    public WorkflowTaskDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Task ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required")
            .Length(1, 100).WithMessage("Task name must be between 1 and 100 characters");

        RuleFor(x => x.AgentType)
            .NotEmpty().WithMessage("Agent type is required")
            .Must(t => t is "http" or "python" or "sql" or "email" or "llm")
            .WithMessage("Agent type must be 'http', 'python', 'sql', 'email', or 'llm'");

        RuleFor(x => x.Prompt)
            .NotEmpty().WithMessage("Task prompt is required for LLM agent type")
            .When(x => x.AgentType == "llm");

        RuleFor(x => x.Prompt)
            .MaximumLength(5000).WithMessage("Task prompt cannot exceed 5000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Prompt));
    }
}