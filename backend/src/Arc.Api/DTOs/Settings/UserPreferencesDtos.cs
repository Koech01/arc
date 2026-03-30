namespace Arc.Api.DTOs.Settings;


/// <summary>
/// Response DTO for user preferences.
/// </summary>
public sealed record UserPreferencesResponseDto
{
    public required string Theme { get; init; }
    public required NotificationPreferencesDto Notifications { get; init; }
    public required string Language { get; init; }
    public required string Timezone { get; init; }
}

/// <summary>
/// Request DTO for updating user preferences.
/// </summary>
public sealed record UpdateUserPreferencesRequestDto
{
    public required string Theme { get; init; }
    public required NotificationPreferencesDto Notifications { get; init; }
    public required string Language { get; init; }
    public required string Timezone { get; init; }
}

/// <summary>
/// Notification preferences nested DTO.
/// </summary>
public sealed record NotificationPreferencesDto
{
    public bool Email { get; init; } = false;
    public bool Push { get; init; } = false;
    public bool ExecutionComplete { get; init; } = false;
    public bool ExecutionFailed { get; init; } = false;

    // For backward compatibility with tests and validators
    public bool EmailEnabled
    {
        get => Email;
        init => Email = value;
    }
    public bool PushEnabled
    {
        get => Push;
        init => Push = value;
    }

    // Support object initializers for required properties in tests
    public NotificationPreferencesDto() {}
    public NotificationPreferencesDto(bool email, bool push, bool executionComplete, bool executionFailed)
    {
        Email = email;
        Push = push;
        ExecutionComplete = executionComplete;
        ExecutionFailed = executionFailed;
    }
}