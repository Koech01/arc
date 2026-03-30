namespace Arc.Api.DTOs.Notifications;


/// <summary>
/// Response DTO for notification details.
/// </summary>
public sealed record NotificationResponseDto
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string Type { get; init; }
    public required bool Read { get; init; }
    public required DateTime CreatedAt { get; init; }
}
