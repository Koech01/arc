using FluentAssertions;
using Arc.Domain.Models;
namespace Arc.UnitTests.Identity;
using Arc.Infrastructure.Identity;


public sealed class SqlitePasswordResetRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqlitePasswordResetRepository _repository;
    private readonly UserId _testUserId;

    public SqlitePasswordResetRepositoryTests()
    {
        _testDbPath = $"./test_password_reset_{Guid.NewGuid()}.db";
        _repository = new SqlitePasswordResetRepository(_testDbPath);
        _testUserId = UserId.From(Guid.NewGuid());
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }

    [Fact]
    public async Task CreateAsync_WithValidToken_ShouldPersistToken()
    {
        // Arrange
        var token = PasswordResetToken.Create(_testUserId, Guid.NewGuid().ToString());

        // Act
        await _repository.CreateAsync(token);

        // Assert
        var retrieved = await _repository.GetByTokenAsync(token.Token);
        retrieved.Should().NotBeNull();
        retrieved!.UserId.Should().Be(_testUserId);
        retrieved.Token.Should().Be(token.Token);
    }

    [Fact]
    public async Task GetByTokenAsync_WithExistingToken_ShouldReturnToken()
    {
        // Arrange
        var token = PasswordResetToken.Create(_testUserId, Guid.NewGuid().ToString());
        await _repository.CreateAsync(token);

        // Act
        var result = await _repository.GetByTokenAsync(token.Token);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(_testUserId);
        result.Token.Should().Be(token.Token);
        result.Used.Should().BeFalse();
    }

    [Fact]
    public async Task GetByTokenAsync_WithNonExistingToken_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByTokenAsync("nonexisting-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTokenUsedStatus()
    {
        // Arrange
        var token = PasswordResetToken.Create(_testUserId, Guid.NewGuid().ToString());
        await _repository.CreateAsync(token);

        // Act
        token = token.MarkAsUsed();
        await _repository.UpdateAsync(token);

        // Assert
        var retrieved = await _repository.GetByTokenAsync(token.Token);
        retrieved.Should().NotBeNull();
        retrieved!.Used.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteExpiredTokensAsync_ShouldRemoveExpiredTokens()
    {
        // Arrange
        var expiredToken = new PasswordResetToken(
            Guid.NewGuid(),
            _testUserId,
            "expired-token",
            DateTime.UtcNow.AddHours(-2), // Expired
            false,
            DateTime.UtcNow.AddHours(-3)
        );

        var validToken = PasswordResetToken.Create(_testUserId, Guid.NewGuid().ToString());

        await _repository.CreateAsync(expiredToken);
        await _repository.CreateAsync(validToken);

        // Act
        await _repository.DeleteExpiredTokensAsync();

        // Assert
        var expiredResult = await _repository.GetByTokenAsync(expiredToken.Token);
        var validResult = await _repository.GetByTokenAsync(validToken.Token);

        expiredResult.Should().BeNull();
        validResult.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateToken_ShouldThrowException()
    {
        // Arrange
        var token = PasswordResetToken.Create(_testUserId, Guid.NewGuid().ToString());
        await _repository.CreateAsync(token);

        // Create a duplicate token with same token string but different ID
        var duplicateToken = new PasswordResetToken(
            Guid.NewGuid(),
            _testUserId,
            token.Token,
            DateTime.UtcNow.AddHours(1),
            false,
            DateTime.UtcNow
        );

        // Act & Assert
        var act = async () => await _repository.CreateAsync(duplicateToken);
        await act.Should().ThrowAsync<Exception>(); // SQLite will throw unique constraint violation
    }
}