using FluentValidation;
using Arc.Api.DTOs.Settings;
namespace Arc.Api.Validators.Settings;

/// <summary>
/// Validator for user preferences update requests.
/// </summary>
public sealed class UpdateUserPreferencesRequestDtoValidator : AbstractValidator<UpdateUserPreferencesRequestDto>
{
    public UpdateUserPreferencesRequestDtoValidator()
    {
        RuleFor(x => x.Theme)
            .NotEmpty()
            .WithMessage("Theme is required")
            .Must(BeValidTheme)
            .WithMessage("Theme must be 'light', 'dark', or 'system'");

        RuleFor(x => x.Language)
            .NotEmpty()
            .WithMessage("Language is required")
            .MaximumLength(10)
            .WithMessage("Language code cannot exceed 10 characters");

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .WithMessage("Timezone is required")
            .MaximumLength(50)
            .WithMessage("Timezone cannot exceed 50 characters");

        RuleFor(x => x.Notifications)
            .NotNull()
            .WithMessage("Notifications preferences are required");
    }

    private static bool BeValidTheme(string theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
            return false;

        var normalizedTheme = theme.ToLowerInvariant();
        return normalizedTheme is "light" or "dark" or "system";
    }
}