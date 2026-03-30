using FluentAssertions;
using Arc.Domain.Models;
namespace Arc.UnitTests.Domain;


public sealed class UserPreferencesTests
{
    private readonly UserId _testUserId = new(Guid.NewGuid());

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreatePreferences()
    {
        var prefs = new UserPreferences(
            _testUserId,
            "dark",
            true,
            false,
            true,
            true,
            "en",
            "UTC");

        prefs.UserId.Should().Be(_testUserId);
        prefs.Theme.Should().Be("dark");
        prefs.NotificationEmail.Should().BeTrue();
        prefs.NotificationPush.Should().BeFalse();
        prefs.NotificationExecutionComplete.Should().BeTrue();
        prefs.NotificationExecutionFailed.Should().BeTrue();
        prefs.Language.Should().Be("en");
        prefs.Timezone.Should().Be("UTC");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyTheme_ShouldThrowException(string theme)
    {
        var act = () => new UserPreferences(_testUserId, theme, true, false, true, true, "en", "UTC");

        act.Should().Throw<ArgumentException>()
           .WithParameterName("theme");
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    [InlineData("system")]
    [InlineData("Light")]
    [InlineData("Dark")]
    [InlineData("System")]
    [InlineData("LIGHT")]
    public void Constructor_WithValidThemes_ShouldNormalizeToLowercase(string theme)
    {
        var prefs = new UserPreferences(_testUserId, theme, true, false, true, true, "en", "UTC");

        prefs.Theme.Should().Be(theme.ToLowerInvariant());
    }

    [Fact]
    public void Constructor_WithInvalidTheme_ShouldThrowException()
    {
        var act = () => new UserPreferences(_testUserId, "invalid", true, false, true, true, "en", "UTC");

        act.Should().Throw<ArgumentException>()
           .WithParameterName("theme")
           .WithMessage("*Invalid theme*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyLanguage_ShouldThrowException(string language)
    {
        var act = () => new UserPreferences(_testUserId, "dark", true, false, true, true, language, "UTC");

        act.Should().Throw<ArgumentException>()
           .WithParameterName("language");
    }

    [Fact]
    public void Constructor_WithLanguageTooLong_ShouldThrowException()
    {
        var act = () => new UserPreferences(_testUserId, "dark", true, false, true, true, new string('a', 11), "UTC");

        act.Should().Throw<ArgumentException>()
           .WithParameterName("language")
           .WithMessage("*cannot exceed 10 characters*");
    }

    [Fact]
    public void Constructor_WithValidLanguage_ShouldNormalizeToLowercase()
    {
        var prefs = new UserPreferences(_testUserId, "dark", true, false, true, true, "EN-US", "UTC");

        prefs.Language.Should().Be("en-us");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyTimezone_ShouldThrowException(string timezone)
    {
        var act = () => new UserPreferences(_testUserId, "dark", true, false, true, true, "en", timezone);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("timezone");
    }

    [Fact]
    public void Constructor_WithTimezoneTooLong_ShouldThrowException()
    {
        var act = () => new UserPreferences(_testUserId, "dark", true, false, true, true, "en", new string('a', 51));

        act.Should().Throw<ArgumentException>()
           .WithParameterName("timezone")
           .WithMessage("*cannot exceed 50 characters*");
    }

    [Fact]
    public void CreateDefault_ShouldCreateDefaultPreferences()
    {
        var prefs = UserPreferences.CreateDefault(_testUserId);

        prefs.UserId.Should().Be(_testUserId);
        prefs.Theme.Should().Be("system");
        prefs.NotificationEmail.Should().BeTrue();
        prefs.NotificationPush.Should().BeFalse();
        prefs.NotificationExecutionComplete.Should().BeTrue();
        prefs.NotificationExecutionFailed.Should().BeTrue();
        prefs.Language.Should().Be("en");
        prefs.Timezone.Should().Be("UTC");
    }

    [Fact]
    public void Update_ShouldCreateNewPreferencesWithUpdatedValues()
    {
        var prefs = UserPreferences.CreateDefault(_testUserId);

        var updated = prefs.Update(
            "dark",
            false,
            true,
            false,
            true,
            "es",
            "America/New_York");

        updated.UserId.Should().Be(_testUserId);
        updated.Theme.Should().Be("dark");
        updated.NotificationEmail.Should().BeFalse();
        updated.NotificationPush.Should().BeTrue();
        updated.NotificationExecutionComplete.Should().BeFalse();
        updated.NotificationExecutionFailed.Should().BeTrue();
        updated.Language.Should().Be("es");
        updated.Timezone.Should().Be("America/New_York");
    }

    [Fact]
    public void Update_WithInvalidTheme_ShouldThrowException()
    {
        var prefs = UserPreferences.CreateDefault(_testUserId);

        var act = () => prefs.Update("invalid", true, false, true, true, "en", "UTC");

        act.Should().Throw<ArgumentException>()
           .WithParameterName("theme");
    }
}