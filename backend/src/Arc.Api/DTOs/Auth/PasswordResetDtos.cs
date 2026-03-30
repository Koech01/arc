namespace Arc.Api.DTOs.Auth;

public sealed record ForgotPasswordRequestDto
{
    public required string Email { get; init; }
}

public sealed record ResetPasswordRequestDto
{
    public required string Token { get; init; }
    public required string NewPassword { get; init; }
}