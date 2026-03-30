namespace Arc.Domain.Models;


/// <summary>
/// Represents user preferences for UI and notification settings.
/// Preferences are immutable and validated at construction.
/// </summary>
public sealed class UserPreferences
{
    public UserId UserId { get; }
    public string Theme { get; }
    public bool NotificationEmail { get; }
    public bool NotificationPush { get; }
    public bool NotificationExecutionComplete { get; }
    public bool NotificationExecutionFailed { get; }
    public string Language { get; }
    public string Timezone { get; }

    public UserPreferences(
        UserId userId,
        string theme,
        bool notificationEmail,
        bool notificationPush,
        bool notificationExecutionComplete,
        bool notificationExecutionFailed,
        string language,
        string timezone)
    {
        if (string.IsNullOrWhiteSpace(theme))
            throw new ArgumentException("Theme cannot be null or empty", nameof(theme));

        if (!IsValidTheme(theme))
            throw new ArgumentException($"Invalid theme: {theme}. Must be 'light', 'dark', or 'system'", nameof(theme));

        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be null or empty", nameof(language));

        if (language.Length > 10)
            throw new ArgumentException("Language code cannot exceed 10 characters", nameof(language));

        if (string.IsNullOrWhiteSpace(timezone))
            throw new ArgumentException("Timezone cannot be null or empty", nameof(timezone));

        if (timezone.Length > 50)
            throw new ArgumentException("Timezone cannot exceed 50 characters", nameof(timezone));

        UserId = userId;
        Theme = theme.ToLowerInvariant();
        NotificationEmail = notificationEmail;
        NotificationPush = notificationPush;
        NotificationExecutionComplete = notificationExecutionComplete;
        NotificationExecutionFailed = notificationExecutionFailed;
        Language = language.ToLowerInvariant();
        Timezone = timezone;
    }

    /// <summary>
    /// Creates default preferences for a new user.
    /// </summary>
    public static UserPreferences CreateDefault(UserId userId)
    {
        return new UserPreferences(
            userId,
            theme: "system",
            notificationEmail: true,
            notificationPush: false,
            notificationExecutionComplete: true,
            notificationExecutionFailed: true,
            language: "en",
            timezone: "UTC");
    }

    /// <summary>
    /// Updates preferences with new values.
    /// </summary>
    public UserPreferences Update(
        string theme,
        bool notificationEmail,
        bool notificationPush,
        bool notificationExecutionComplete,
        bool notificationExecutionFailed,
        string language,
        string timezone)
    {
        return new UserPreferences(
            UserId,
            theme,
            notificationEmail,
            notificationPush,
            notificationExecutionComplete,
            notificationExecutionFailed,
            language,
            timezone);
    }

    private static bool IsValidTheme(string theme)
    {
        var normalizedTheme = theme.ToLowerInvariant();
        return normalizedTheme is "light" or "dark" or "system";
    }
}