using FluentAssertions;
namespace Arc.UnitTests.Identity;
using Arc.Infrastructure.Identity;


public sealed class BCryptPasswordHashingServiceTests
{
    private readonly BCryptPasswordHashingService _passwordHashingService;

    public BCryptPasswordHashingServiceTests()
    {
        _passwordHashingService = new BCryptPasswordHashingService();
    }

    [Fact]
    public void HashPassword_WithValidPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var hashedPassword = _passwordHashingService.HashPassword(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password);
        hashedPassword.Should().StartWith("$2a$"); // BCrypt hash format
    }

    [Fact]
    public void HashPassword_WithSamePassword_ShouldReturnDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var hash1 = _passwordHashingService.HashPassword(password);
        var hash2 = _passwordHashingService.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt uses random salt
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HashPassword_WithInvalidPassword_ShouldThrowArgumentException(string invalidPassword)
    {
        // Act & Assert
        var act = () => _passwordHashingService.HashPassword(invalidPassword);
        act.Should().Throw<ArgumentException>()
           .WithParameterName("password");
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123";
        var hashedPassword = _passwordHashingService.HashPassword(password);

        // Act
        var result = _passwordHashingService.VerifyPassword(password, hashedPassword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123";
        var wrongPassword = "WrongPassword456";
        var hashedPassword = _passwordHashingService.HashPassword(password);

        // Act
        var result = _passwordHashingService.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "valid_hash")]
    [InlineData(" ", "valid_hash")]
    [InlineData(null, "valid_hash")]
    public void VerifyPassword_WithInvalidPassword_ShouldReturnFalse(string invalidPassword, string hashedPassword)
    {
        // Act
        var result = _passwordHashingService.VerifyPassword(invalidPassword, hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("valid_password", "")]
    [InlineData("valid_password", " ")]
    [InlineData("valid_password", null)]
    public void VerifyPassword_WithInvalidHashedPassword_ShouldReturnFalse(string password, string invalidHashedPassword)
    {
        // Act
        var result = _passwordHashingService.VerifyPassword(password, invalidHashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithInvalidHashFormat_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123";
        var invalidHash = "not_a_valid_bcrypt_hash";

        // Act
        var result = _passwordHashingService.VerifyPassword(password, invalidHash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ShouldBeDeterministicForVerification()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var hashedPassword = _passwordHashingService.HashPassword(password);
        var verification1 = _passwordHashingService.VerifyPassword(password, hashedPassword);
        var verification2 = _passwordHashingService.VerifyPassword(password, hashedPassword);

        // Assert
        verification1.Should().BeTrue();
        verification2.Should().BeTrue();
        verification1.Should().Be(verification2);
    }
}