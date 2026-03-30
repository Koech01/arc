using FluentAssertions;
using Arc.Domain.Models;
namespace Arc.UnitTests.Domain;


public sealed class UserTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateUser()
    {
        // Arrange
        var id = UserId.From(Guid.NewGuid());
        var username = "testuser";
        var email = "test@example.com";
        var passwordHash = "hashed_password";
        var role = UserRole.User;
        var createdAt = DateTime.UtcNow;

        // Act
        var user = new User(id, username, email, passwordHash, role, createdAt);

        // Assert
        user.Id.Should().Be(id);
        user.Username.Should().Be(username);
        user.Email.Should().Be(email.ToLowerInvariant());
        user.PasswordHash.Should().Be(passwordHash);
        user.Role.Should().Be(role);
        user.CreatedAt.Should().Be(createdAt);
        user.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithInvalidEmail_ShouldThrowArgumentException(string invalidEmail)
    {
        // Arrange
          var id = UserId.From(Guid.NewGuid());
          var username = "testuser";
          var passwordHash = "hashed_password";
          var role = UserRole.User;
          var createdAt = DateTime.UtcNow;

          // Act & Assert
          var act = () => new User(id, username, invalidEmail, passwordHash, role, createdAt);
          act.Should().Throw<ArgumentException>()
              .WithParameterName("email");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithInvalidPasswordHash_ShouldThrowArgumentException(string invalidPasswordHash)
    {
        // Arrange
          var id = UserId.From(Guid.NewGuid());
          var username = "testuser";
          var email = "test@example.com";
          var role = UserRole.User;
          var createdAt = DateTime.UtcNow;

          // Act & Assert
          var act = () => new User(id, username, email, invalidPasswordHash, role, createdAt);
          act.Should().Throw<ArgumentException>()
              .WithParameterName("passwordHash");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public void Constructor_WithInvalidEmailFormat_ShouldThrowArgumentException(string invalidEmail)
    {
        // Arrange
          var id = UserId.From(Guid.NewGuid());
          var username = "testuser";
          var passwordHash = "hashed_password";
          var role = UserRole.User;
          var createdAt = DateTime.UtcNow;

          // Act & Assert
          var act = () => new User(id, username, invalidEmail, passwordHash, role, createdAt);
          act.Should().Throw<ArgumentException>()
              .WithParameterName("email");
    }

    [Fact]
    public void Create_WithValidParameters_ShouldCreateUserWithGeneratedId()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";
        var passwordHash = "hashed_password";
        var role = UserRole.Admin;

        // Act
        var user = User.Create(username, email, passwordHash, role);

        // Assert
        user.Id.Value.Should().NotBeEmpty();
        user.Username.Should().Be(username);
        user.Email.Should().Be(email.ToLowerInvariant());
        user.PasswordHash.Should().Be(passwordHash);
        user.Role.Should().Be(role);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldReturnUserWithIsActiveFalse()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hashed_password", UserRole.User);

        // Act
        var deactivatedUser = user.Deactivate();

        // Assert
        deactivatedUser.Id.Should().Be(user.Id);
        deactivatedUser.Email.Should().Be(user.Email);
        deactivatedUser.PasswordHash.Should().Be(user.PasswordHash);
        deactivatedUser.Role.Should().Be(user.Role);
        deactivatedUser.CreatedAt.Should().Be(user.CreatedAt);
        deactivatedUser.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldReturnUserWithIsActiveTrue()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hashed_password", UserRole.User).Deactivate();

        // Act
        var activatedUser = user.Activate();

        // Assert
        activatedUser.Id.Should().Be(user.Id);
        activatedUser.Email.Should().Be(user.Email);
        activatedUser.PasswordHash.Should().Be(user.PasswordHash);
        activatedUser.Role.Should().Be(user.Role);
        activatedUser.CreatedAt.Should().Be(user.CreatedAt);
        activatedUser.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldNormalizeEmailToLowerCase()
    {
        // Arrange
        var id = UserId.From(Guid.NewGuid());
        var username = "testuser";
        var email = "TEST@EXAMPLE.COM";
        var passwordHash = "hashed_password";
        var role = UserRole.User;
        var createdAt = DateTime.UtcNow;

        // Act
        var user = new User(id, username, email, passwordHash, role, createdAt);

        // Assert
        user.Email.Should().Be("test@example.com");
    }
}