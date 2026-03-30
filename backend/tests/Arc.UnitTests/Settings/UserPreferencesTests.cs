using Arc.Domain.Models; 
namespace Arc.UnitTests.Settings;


/// <summary>
/// Unit tests for UserPreferences domain model.
/// Validates domain invariants and business rules.
/// </summary>
public sealed class UserPreferencesTests
{
    [Fact]
    public void CreateDefault_CreatesPreferencesWithDefaultValues()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());

        // Act
        var preferences = UserPreferences.CreateDefault(userId);

        // Assert
        Assert.Equal(userId, preferences.UserId);
        Assert.Equal("system", preferences.Theme);
        Assert.True(preferences.NotificationEmail);
        Assert.False(preferences.NotificationPush);
        Assert.True(preferences.NotificationExecutionComplete);
        Assert.True(preferences.NotificationExecutionFailed);
        Assert.Equal("en", preferences.Language);
        Assert.Equal("UTC", preferences.Timezone);
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    [InlineData("system")]
    [InlineData("LIGHT")]
    [InlineData("Dark")]
    public void Constructor_WithValidTheme_CreatesPreferences(string theme)
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());

        // Act
        var preferences = new UserPreferences(userId, theme, true, false, true, true, "en", "UTC");

        // Assert
        Assert.Equal(theme.ToLowerInvariant(), preferences.Theme);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("blue")]
    public void Constructor_WithInvalidTheme_ThrowsArgumentException(string theme)
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new UserPreferences(userId, theme, true, false, true, true, "en", "UTC"));
    }

    [Fact]
    public void Constructor_WithEmptyLanguage_ThrowsArgumentException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new UserPreferences(userId, "light", true, false, true, true, "", "UTC"));
    }

    [Fact]
    public void Constructor_WithLanguageTooLong_ThrowsArgumentException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var longLanguage = new string('a', 11);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new UserPreferences(userId, "light", true, false, true, true, longLanguage, "UTC"));
    }

    [Fact]
    public void Constructor_WithEmptyTimezone_ThrowsArgumentException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new UserPreferences(userId, "light", true, false, true, true, "en", ""));
    }

    [Fact]
    public void Constructor_WithTimezoneTooLong_ThrowsArgumentException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var longTimezone = new string('a', 51);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new UserPreferences(userId, "light", true, false, true, true, "en", longTimezone));
    }

    [Fact]
    public void Update_ReturnsNewInstanceWithUpdatedValues()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var original = UserPreferences.CreateDefault(userId);

        // Act
        var updated = original.Update(
            "dark",
            false,
            true,
            false,
            false,
            "es",
            "America/New_York");

        // Assert
        Assert.NotSame(original, updated);
        Assert.Equal(userId, updated.UserId);
        Assert.Equal("dark", updated.Theme);
        Assert.False(updated.NotificationEmail);
        Assert.True(updated.NotificationPush);
        Assert.False(updated.NotificationExecutionComplete);
        Assert.False(updated.NotificationExecutionFailed);
        Assert.Equal("es", updated.Language);
        Assert.Equal("America/New_York", updated.Timezone);

        // Original unchanged
        Assert.Equal("system", original.Theme);
        Assert.True(original.NotificationEmail);
    }

    [Fact]
    public void Constructor_NormalizesThemeToLowerCase()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());

        // Act
        var preferences = new UserPreferences(userId, "DARK", true, false, true, true, "EN", "UTC");

        // Assert
        Assert.Equal("dark", preferences.Theme);
        Assert.Equal("en", preferences.Language);
    }
}