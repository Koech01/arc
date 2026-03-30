using NSubstitute;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Domain.Exceptions;
using Arc.Application.Admin;
using Arc.Application.Identity;
using Microsoft.Extensions.Logging;


namespace Arc.UnitTests.Identity;
public sealed class DeterministicAuthenticationServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly ILogger<DeterministicAuthenticationService> _logger;
    private readonly DeterministicAuthenticationService _authenticationService;

    public DeterministicAuthenticationServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _passwordHashingService = Substitute.For<IPasswordHashingService>();
        _loginHistoryRepository = Substitute.For<ILoginHistoryRepository>();
        _logger = Substitute.For<ILogger<DeterministicAuthenticationService>>();
        _authenticationService = new DeterministicAuthenticationService(
            _userRepository, _passwordHashingService, _loginHistoryRepository, _logger);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123";
        var role = UserRole.User;
        var hashedPassword = "hashed_password";

        _userRepository.ExistsByEmailAsync(email).Returns(false);
        _passwordHashingService.HashPassword(password).Returns(hashedPassword);
        _userRepository.CreateAsync(Arg.Any<User>()).Returns(callInfo => callInfo.Arg<User>());

        // Act
        var username = "testuser";
        var result = await _authenticationService.RegisterAsync(username, email, password, role);

        // Assert
        result.Email.Should().Be(email);
        result.PasswordHash.Should().Be(hashedPassword);
        result.Role.Should().Be(role);
        result.IsActive.Should().BeTrue();

        await _userRepository.Received(1).ExistsByEmailAsync(email);
        _passwordHashingService.Received(1).HashPassword(password);
        await _userRepository.Received(1).CreateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowAuthenticationException()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123";

        _userRepository.ExistsByEmailAsync(email).Returns(true);

        // Act & Assert
        var act = () => _authenticationService.RegisterAsync(email, email, password, UserRole.User);
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage($"User with email '{email}' already exists");

        await _userRepository.Received(1).ExistsByEmailAsync(email);
        _passwordHashingService.DidNotReceive().HashPassword(Arg.Any<string>());
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<User>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task RegisterAsync_WithInvalidEmail_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act & Assert
        var act = () => _authenticationService.RegisterAsync(invalidEmail, "Password123");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("email");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("1234567")] // Too short
    public async Task RegisterAsync_WithInvalidPassword_ShouldThrowArgumentException(string invalidPassword)
    {
        // Act & Assert
        var act = () => _authenticationService.RegisterAsync("test@example.com", invalidPassword);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("password");
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnUser()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123";
            var user = User.Create("testuser", email, "hashed_password", UserRole.User);

        _userRepository.GetByEmailAsync(email).Returns(user);
        _passwordHashingService.VerifyPassword(password, user.PasswordHash).Returns(true);

        // Act
        var result = await _authenticationService.AuthenticateAsync(email, password);

        // Assert
        result.Should().BeEquivalentTo(user);

        await _userRepository.Received(1).GetByEmailAsync(email);
        _passwordHashingService.Received(1).VerifyPassword(password, user.PasswordHash);
    }

    [Fact]
    public async Task AuthenticateAsync_WithNonExistentUser_ShouldThrowAuthenticationException()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123";

        _userRepository.GetByEmailAsync(email).Returns((User?)null);

        // Act & Assert
        var act = () => _authenticationService.AuthenticateAsync(email, password);
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("Invalid email or password");

        await _userRepository.Received(1).GetByEmailAsync(email);
        _passwordHashingService.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task AuthenticateAsync_WithInactiveUser_ShouldThrowAuthenticationException()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123";
            var user = User.Create("testuser", email, "hashed_password", UserRole.User).Deactivate();

        _userRepository.GetByEmailAsync(email).Returns(user);

        // Act & Assert
        var act = () => _authenticationService.AuthenticateAsync(email, password);
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("User account is inactive");

        await _userRepository.Received(1).GetByEmailAsync(email);
        _passwordHashingService.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ShouldThrowAuthenticationException()
    {
        // Arrange
        var email = "test@example.com";
        var password = "WrongPassword";
        var user = User.Create(email, "hashed_password", UserRole.User);

        _userRepository.GetByEmailAsync(email).Returns(user);
        _passwordHashingService.VerifyPassword(password, user.PasswordHash).Returns(false);

        // Act & Assert
        var act = () => _authenticationService.AuthenticateAsync(email, password);
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("Invalid email or password");

        await _userRepository.Received(1).GetByEmailAsync(email);
        _passwordHashingService.Received(1).VerifyPassword(password, user.PasswordHash);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var user = User.Create("test@example.com", "hashed_password", UserRole.User);

        _userRepository.GetByIdAsync(userId).Returns(user);

        // Act
        var result = await _authenticationService.GetUserByIdAsync(userId);

        // Assert
        result.Should().Be(user);
        await _userRepository.Received(1).GetByIdAsync(userId);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithValidEmail_ShouldReturnUser()
    {
        // Arrange
        var email = "test@example.com";
            var user = User.Create("testuser", email, "hashed_password", UserRole.User);

        _userRepository.GetByEmailAsync(email).Returns(user);

        // Act
        var result = await _authenticationService.GetUserByEmailAsync(email);

        // Assert
        result.Should().Be(user);
        await _userRepository.Received(1).GetByEmailAsync(email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetUserByEmailAsync_WithInvalidEmail_ShouldReturnNull(string invalidEmail)
    {
        // Act
        var result = await _authenticationService.GetUserByEmailAsync(invalidEmail);

        // Assert
        result.Should().BeNull();
        await _userRepository.DidNotReceive().GetByEmailAsync(Arg.Any<string>());
    }
}