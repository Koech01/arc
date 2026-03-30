using FluentAssertions;
using Arc.Api.DTOs.Settings;
using Arc.Api.Validators.Settings;
namespace Arc.UnitTests.Validators;


public sealed class UpdateUserPreferencesRequestDtoValidatorTests
{
    private readonly UpdateUserPreferencesRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var request = new UpdateUserPreferencesRequestDto
        {
            Theme = "dark",
            Language = "en",
            Timezone = "UTC",
            Notifications = new NotificationPreferencesDto { Email = true, Push = false, ExecutionComplete = true, ExecutionFailed = true }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyTheme_ShouldFail(string theme)
    {
        var request = new UpdateUserPreferencesRequestDto
        {
            Theme = theme,
            Language = "en",
            Timezone = "UTC",
            Notifications = new NotificationPreferencesDto { Email = true, Push = false, ExecutionComplete = true, ExecutionFailed = true }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Theme" && 
                                            e.ErrorMessage == "Theme is required");
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    [InlineData("system")]
    [InlineData("Light")]
    [InlineData("Dark")]
    [InlineData("System")]
    [InlineData("LIGHT")]
    public void Validate_WithValidThemes_ShouldPass(string theme)
    {
        var request = new UpdateUserPreferencesRequestDto
        {
            Theme = theme,
            Language = "en",
            Timezone = "UTC",
            Notifications = new NotificationPreferencesDto { EmailEnabled = true, PushEnabled = false }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidTheme_ShouldFail()
    {
        var request = new UpdateUserPreferencesRequestDto
        {
            Theme = "invalid",
            Language = "en",
            Timezone = "UTC",
            Notifications = new NotificationPreferencesDto { EmailEnabled = true, PushEnabled = false }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Theme" && 
                                            e.ErrorMessage == "Theme must be 'light', 'dark', or 'system'");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyLanguage_ShouldFail(string language)
    {
        var request = new UpdateUserPreferencesRequestDto
        {
            Theme = "dark",
            Language = language,
            Timezone = "UTC",
            Notifications = new NotificationPreferencesDto { EmailEnabled = true, PushEnabled = false }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Language" && 
                                            e.ErrorMessage == "Language is required");
    }

    [Fact]
    public void Validate_WithLanguageTooLong_ShouldFail()
    {
        var request = new UpdateUserPreferencesRequestDto
        {
            Theme = "dark",
            Language = new string('a', 11),
            Timezone = "UTC",
            Notifications = new NotificationPreferencesDto { EmailEnabled = true, PushEnabled = false }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Language" && 
                                            e.ErrorMessage == "Language code cannot exceed 10 characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyTimezone_ShouldFail(string timezone)
    {
        var request = new UpdateUserPreferencesRequestDto
        {
            Theme = "dark",
            Language = "en",
            Timezone = timezone,
            Notifications = new NotificationPreferencesDto { EmailEnabled = true, PushEnabled = false }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Timezone" && 
                                            e.ErrorMessage == "Timezone is required");
    }

    [Fact]
    public void Validate_WithTimezoneTooLong_ShouldFail()
    {
        var request = new UpdateUserPreferencesRequestDto
        {
            Theme = "dark",
            Language = "en",
            Timezone = new string('a', 51),
            Notifications = new NotificationPreferencesDto { EmailEnabled = true, PushEnabled = false }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Timezone" && 
                                            e.ErrorMessage == "Timezone cannot exceed 50 characters");
    }

    [Fact]
    public void Validate_WithNullNotifications_ShouldFail()
    {
        var request = new UpdateUserPreferencesRequestDto
        {
            Theme = "dark",
            Language = "en",
            Timezone = "UTC",
            Notifications = null!
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notifications" && 
                                            e.ErrorMessage == "Notifications preferences are required");
    }
}