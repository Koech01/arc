namespace Arc.Api.DTOs.Auth;


/// <summary>
/// Request DTO for user registration.
/// </summary>
public sealed record RegisterRequestDto
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? Role { get; init; }
}

/// <summary>
/// Request DTO for user login.
/// </summary>
public sealed record LoginRequestDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

/// <summary>
/// Response DTO for successful authentication.
/// </summary>
public sealed record AuthResponseDto
{
    public required UserDto User { get; init; }
}

/// <summary>
/// DTO representing user information.
/// </summary>
public sealed record UserDto
{
    public required string Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public string? Firstname { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsActive { get; init; }
}

/// <summary>
/// Request DTO for updating user profile.
/// </summary>
public sealed record UpdateProfileRequestDto
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public string? Firstname { get; init; }
}