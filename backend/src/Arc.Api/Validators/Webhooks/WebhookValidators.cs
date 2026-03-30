using FluentValidation;
using Arc.Api.DTOs.Webhooks;
namespace Arc.Api.Validators.Webhooks;


public sealed class CreateWebhookRequestDtoValidator : AbstractValidator<CreateWebhookRequestDto>
{
    public CreateWebhookRequestDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Webhook URL is required")
            .Must(BeAValidUrl).WithMessage("Webhook URL must be a valid HTTP/HTTPS URL");

        RuleFor(x => x.Events)
            .NotEmpty().WithMessage("At least one event type must be selected")
            .Must(events => events.All(IsValidEventType))
            .WithMessage("All event types must be one of: execution.started, execution.completed, execution.failed");

        RuleFor(x => x.Secret)
            .NotEmpty().WithMessage("Webhook secret is required")
            .MinimumLength(20).WithMessage("Webhook secret must be at least 20 characters");
    }

    private static bool BeAValidUrl(string url)
    {
        try
        {
            var uri = new Uri(url, UriKind.Absolute);
            return uri.Scheme is "http" or "https";
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidEventType(string eventType)
    {
        return eventType is "execution.started" or "execution.completed" or "execution.failed";
    }
}

/// <summary>
/// Validates PATCH /api/webhooks/{id} requests.
/// Secret is optional - if provided it must meet the minimum length requirement.
/// </summary>
public sealed class UpdateWebhookRequestDtoValidator : AbstractValidator<UpdateWebhookRequestDto>
{
    public UpdateWebhookRequestDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Webhook URL is required")
            .Must(BeAValidUrl).WithMessage("Webhook URL must be a valid HTTP/HTTPS URL");

        RuleFor(x => x.Events)
            .NotEmpty().WithMessage("At least one event type must be selected")
            .Must(events => events.All(IsValidEventType))
            .WithMessage("All event types must be one of: execution.started, execution.completed, execution.failed");

        // Secret is optional for PATCH; validate only when a value is actually supplied.
        When(x => !string.IsNullOrEmpty(x.Secret), () =>
        {
            RuleFor(x => x.Secret)
                .MinimumLength(20).WithMessage("Webhook secret must be at least 20 characters");
        });
    }

    private static bool BeAValidUrl(string url)
    {
        try
        {
            var uri = new Uri(url, UriKind.Absolute);
            return uri.Scheme is "http" or "https";
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidEventType(string eventType)
    {
        return eventType is "execution.started" or "execution.completed" or "execution.failed";
    }
}