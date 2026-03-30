using FluentAssertions;
using Arc.Domain.Models;
using Arc.Infrastructure.Identity;


namespace Arc.UnitTests.Identity;
public sealed class SqliteUserRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqliteUserRepository _repository;

    public SqliteUserRepositoryTests()
    {
        _testDbPath = $"./test_users_{Guid.NewGuid()}.db";
        _repository = new SqliteUserRepository(_testDbPath);
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }

    [Fact]
    public async Task CreateAsync_WithValidUser_ShouldPersistUser()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hashedpassword", UserRole.User);

        // Act
        var result = await _repository.CreateAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);

        var retrieved = await _repository.GetByIdAsync(user.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task CreateAsync_WithNullUser_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _repository.CreateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ShouldReturnUser()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hashedpassword", UserRole.User);
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("test@example.com");
        result.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = UserId.From(Guid.NewGuid());

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ShouldReturnUser()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hashedpassword", UserRole.User);
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseInsensitive()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hashedpassword", UserRole.User);
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByEmailAsync("TEST@EXAMPLE.COM");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistingEmail_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexisting@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetByEmailAsync_WithInvalidEmail_ShouldReturnNull(string? invalidEmail)
    {
        // Act
        var result = await _repository.GetByEmailAsync(invalidEmail!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingUser_ShouldUpdateUser()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hashedpassword", UserRole.User);
        await _repository.CreateAsync(user);

        var updatedUser = new User(
            user.Id,
            "updated@example.com", // username
            "updated@example.com", // email
            "newhashedpassword",
            UserRole.Admin,
            user.CreatedAt,
            true, // isActive
            null, // firstname
            0, // failedLoginAttempts
            null, // lockedUntilUtc
            null  // deletedAt
        );

        // Act
        await _repository.UpdateAsync(updatedUser);

        // Assert
        var result = await _repository.GetByIdAsync(user.Id);
        result.Should().NotBeNull();
        result!.Email.Should().Be("updated@example.com");
        result.Role.Should().Be(UserRole.Admin);
        result.PasswordHash.Should().Be("newhashedpassword");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hashedpassword", UserRole.User);

        // Act & Assert
        var act = async () => await _repository.UpdateAsync(user);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with ID {user.Id} not found");
    }

    [Fact]
    public async Task UpdateAsync_WithNullUser_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _repository.UpdateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExistsByEmailAsync_WithExistingEmail_ShouldReturnTrue()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hashedpassword", UserRole.User);
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.ExistsByEmailAsync("test@example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByEmailAsync_IsCaseInsensitive()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hashedpassword", UserRole.User);
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.ExistsByEmailAsync("TEST@EXAMPLE.COM");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByEmailAsync_WithNonExistingEmail_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsByEmailAsync("nonexisting@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ExistsByEmailAsync_WithInvalidEmail_ShouldReturnFalse(string? invalidEmail)
    {
        // Act
        var result = await _repository.ExistsByEmailAsync(invalidEmail!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_WithNoFilters_ShouldReturnAllUsers()
    {
        // Arrange
        var user1 = User.Create("user1", "user1@example.com", "hash1", UserRole.User);
        var user2 = User.Create("user2", "user2@example.com", "hash2", UserRole.Admin);
        await _repository.CreateAsync(user1);
        await _repository.CreateAsync(user2);

        // Act
        var (users, totalCount) = await _repository.GetAllAsync(
            emailSearch: null,
            usernameSearch: null,
            role: null,
            isActive: null,
            includeDeleted: false,
            limit: 10,
            offset: 0);

        // Assert
        users.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_WithEmailFilter_ShouldReturnMatchingUsers()
    {
        // Arrange
        var user1 = User.Create("testuser1", "test@example.com", "hash1", UserRole.User);
        var user2 = User.Create("testuser2", "other@example.com", "hash2", UserRole.User);
        await _repository.CreateAsync(user1);
        await _repository.CreateAsync(user2);

        // Act
        var (users, totalCount) = await _repository.GetAllAsync(
            emailSearch: "test",
            usernameSearch: null,
            role: null,
            isActive: null,
            includeDeleted: false,
            limit: 10,
            offset: 0);

        // Assert
        users.Should().HaveCount(1);
        users.First().Email.Should().Contain("test");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_WithRoleFilter_ShouldReturnMatchingUsers()
    {
        // Arrange
        var user1 = User.Create("user1", "user1@example.com", "hash1", UserRole.User);
        var user2 = User.Create("admin", "admin@example.com", "hash2", UserRole.Admin);
        await _repository.CreateAsync(user1);
        await _repository.CreateAsync(user2);

        // Act
        var (users, totalCount) = await _repository.GetAllAsync(
            emailSearch: null,
            usernameSearch: null,
            role: UserRole.Admin,
            isActive: null,
            includeDeleted: false,
            limit: 10,
            offset: 0);

        // Assert
        users.Should().HaveCount(1);
        users.First().Role.Should().Be(UserRole.Admin);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var user = User.Create($"user{i}", $"user{i}@example.com", $"hash{i}", UserRole.User);
            await _repository.CreateAsync(user);
        }

        // Act
        var (users, totalCount) = await _repository.GetAllAsync(
            emailSearch: null,
            usernameSearch: null,
            role: null,
            isActive: null,
            includeDeleted: false,
            limit: 2,
            offset: 2);

        // Assert
        users.Should().HaveCount(2);
        totalCount.Should().Be(5);
    }

    [Fact]
    public async Task Constructor_WithInvalidDatabasePath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => new SqliteUserRepository("");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("databasePath");
    }
}