using NSubstitute;
using Arc.Domain.Models;
using Arc.Application.Admin;
using Arc.Domain.Exceptions;
using Arc.Application.Identity;
using Microsoft.Extensions.Logging;


namespace Arc.UnitTests.Identity;
/// <summary>
/// Unit tests for user profile update functionality.
/// Validates deterministic behavior, email validation, and duplicate detection.
/// </summary>
/// 
public sealed class UpdateProfileTests
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly ILogger<DeterministicAuthenticationService> _logger;
    private readonly DeterministicAuthenticationService _authService;

    public UpdateProfileTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _passwordHashingService = Substitute.For<IPasswordHashingService>();
        _loginHistoryRepository = Substitute.For<ILoginHistoryRepository>();
        _logger = Substitute.For<ILogger<DeterministicAuthenticationService>>();
        _authService = new DeterministicAuthenticationService(_userRepository, _passwordHashingService, _loginHistoryRepository, _logger);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidUsernameAndEmail_UpdatesSuccessfully()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var existingUser = new User(userId, "olduser", "old@example.com", "hash123", UserRole.User, DateTime.UtcNow);
        var updatedUser = existingUser.UpdateProfile("newuser", "new@example.com");

        _userRepository.GetByIdAsync(userId).Returns(existingUser);
        _userRepository.GetByEmailAsync("new@example.com").Returns((User?)null);
        _userRepository.UpdateAsync(Arg.Any<User>()).Returns(updatedUser);

        // Act
        var result = await _authService.UpdateProfileAsync(userId, "newuser", "new@example.com");

        // Assert
        Assert.Equal("newuser", result.Username);
        Assert.Equal("new@example.com", result.Email);
        await _userRepository.Received(1).UpdateAsync(Arg.Is<User>(u => u.Username == "newuser" && u.Email == "new@example.com"));
    }

    [Fact]
    public async Task UpdateProfileAsync_WithDuplicateEmail_ThrowsAuthenticationException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var otherUserId = UserId.From(Guid.NewGuid());
        var existingUser = new User(userId, "user", "user@example.com", "hash123", UserRole.User, DateTime.UtcNow);
        var otherUser = new User(otherUserId, "other", "taken@example.com", "hash456", UserRole.User, DateTime.UtcNow);

        _userRepository.GetByIdAsync(userId).Returns(existingUser);
        _userRepository.GetByEmailAsync("taken@example.com").Returns(otherUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _authService.UpdateProfileAsync(userId, "newuser", "taken@example.com"));
        
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNonExistentUser_ThrowsAuthenticationException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        _userRepository.GetByIdAsync(userId).Returns((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _authService.UpdateProfileAsync(userId, "newuser", "new@example.com"));
        
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithEmptyUsername_ThrowsArgumentException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.UpdateProfileAsync(userId, "", "new@example.com"));
    }

    [Fact]
    public async Task UpdateProfileAsync_WithEmptyEmail_ThrowsArgumentException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.UpdateProfileAsync(userId, "newuser", ""));
    }

    [Fact]
    public async Task UpdateProfileAsync_WithSameUsernameAndEmail_UpdatesSuccessfully()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var existingUser = new User(userId, "user", "user@example.com", "hash123", UserRole.User, DateTime.UtcNow);

        _userRepository.GetByIdAsync(userId).Returns(existingUser);
        _userRepository.GetByEmailAsync("user@example.com").Returns(existingUser);
        _userRepository.UpdateAsync(Arg.Any<User>()).Returns(existingUser);

        // Act
        var result = await _authService.UpdateProfileAsync(userId, "user", "user@example.com");

        // Assert
        Assert.Equal("user", result.Username);
        Assert.Equal("user@example.com", result.Email);
        await _userRepository.Received(1).UpdateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task UpdateProfileAsync_NormalizesEmailToLowerCase()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var existingUser = new User(userId, "user", "user@example.com", "hash123", UserRole.User, DateTime.UtcNow);
        var updatedUser = existingUser.UpdateProfile("newuser", "new@example.com");

        _userRepository.GetByIdAsync(userId).Returns(existingUser);
        _userRepository.GetByEmailAsync("new@example.com").Returns((User?)null);
        _userRepository.UpdateAsync(Arg.Any<User>()).Returns(updatedUser);

        // Act
        var result = await _authService.UpdateProfileAsync(userId, "newuser", "NEW@EXAMPLE.COM");

        // Assert
        Assert.Equal("new@example.com", result.Email);
        await _userRepository.Received(1).GetByEmailAsync("new@example.com");
    }
}