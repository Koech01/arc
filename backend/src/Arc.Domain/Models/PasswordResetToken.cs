namespace Arc.Domain.Models;

public sealed class PasswordResetToken
{
    public Guid Id { get; }
    public UserId UserId { get; }
    public string Token { get; }
    public DateTime ExpiresAtUtc { get; }
    public bool Used { get; }
    public DateTime CreatedAtUtc { get; }

    public PasswordResetToken(Guid id, UserId userId, string token, DateTime expiresAtUtc, bool used, DateTime createdAtUtc)
    {
        Id = id;
        UserId = userId;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        Used = used;
        CreatedAtUtc = createdAtUtc;
    }

    public static PasswordResetToken Create(UserId userId, string token, int expirationMinutes = 15)
    {
        return new PasswordResetToken(
            Guid.NewGuid(),
            userId,
            token,
            DateTime.UtcNow.AddMinutes(expirationMinutes),
            false,
            DateTime.UtcNow
        );
    }

    public PasswordResetToken MarkAsUsed()
    {
        return new PasswordResetToken(Id, UserId, Token, ExpiresAtUtc, true, CreatedAtUtc);
    }

    public bool IsValid() => !Used && ExpiresAtUtc > DateTime.UtcNow;
}