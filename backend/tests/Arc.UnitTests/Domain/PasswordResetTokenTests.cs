using FluentAssertions;
using Arc.Domain.Models;
namespace Arc.UnitTests.Domain;


public sealed class PasswordResetTokenTests
{
    private readonly UserId _testUserId = new(Guid.NewGuid());

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateToken()
    {
        var id = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var createdAt = DateTime.UtcNow;

        var token = new PasswordResetToken(id, _testUserId, "token-string", expiresAt, false, createdAt);

        token.Id.Should().Be(id);
        token.UserId.Should().Be(_testUserId);
        token.Token.Should().Be("token-string");
        token.ExpiresAtUtc.Should().Be(expiresAt);
        token.Used.Should().BeFalse();
        token.CreatedAtUtc.Should().Be(createdAt);
    }

    [Fact]
    public void Create_ShouldCreateTokenWithDefaultExpiration()
    {
        var before = DateTime.UtcNow;

        var token = PasswordResetToken.Create(_testUserId, "token-string");

        var after = DateTime.UtcNow.AddMinutes(15);

        token.Id.Should().NotBeEmpty();
        token.UserId.Should().Be(_testUserId);
        token.Token.Should().Be("token-string");
        token.ExpiresAtUtc.Should().BeOnOrAfter(before.AddMinutes(15)).And.BeOnOrBefore(after);
        token.Used.Should().BeFalse();
        token.CreatedAtUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithCustomExpiration_ShouldUseProvidedMinutes()
    {
        var before = DateTime.UtcNow;

        var token = PasswordResetToken.Create(_testUserId, "token-string", 30);

        var after = DateTime.UtcNow.AddMinutes(30);

        token.ExpiresAtUtc.Should().BeOnOrAfter(before.AddMinutes(30)).And.BeOnOrBefore(after);
    }

    [Fact]
    public void MarkAsUsed_ShouldReturnNewTokenMarkedAsUsed()
    {
        var token = PasswordResetToken.Create(_testUserId, "token-string");

        var usedToken = token.MarkAsUsed();

        usedToken.Used.Should().BeTrue();
        usedToken.Id.Should().Be(token.Id);
        usedToken.UserId.Should().Be(token.UserId);
        usedToken.Token.Should().Be(token.Token);
        usedToken.ExpiresAtUtc.Should().Be(token.ExpiresAtUtc);
        usedToken.CreatedAtUtc.Should().Be(token.CreatedAtUtc);
    }

    [Fact]
    public void IsValid_WhenNotUsedAndNotExpired_ShouldReturnTrue()
    {
        var token = PasswordResetToken.Create(_testUserId, "token-string", 15);

        token.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenUsed_ShouldReturnFalse()
    {
        var token = PasswordResetToken.Create(_testUserId, "token-string", 15);
        var usedToken = token.MarkAsUsed();

        usedToken.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(-1);
        var token = new PasswordResetToken(Guid.NewGuid(), _testUserId, "token-string", expiresAt, false, DateTime.UtcNow);

        token.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenUsedAndExpired_ShouldReturnFalse()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(-1);
        var token = new PasswordResetToken(Guid.NewGuid(), _testUserId, "token-string", expiresAt, true, DateTime.UtcNow);

        token.IsValid().Should().BeFalse();
    }
}