using Moq;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Admin;
using Arc.Application.Identity;
using Arc.Infrastructure.Admin;
using Microsoft.Extensions.Logging;


namespace Arc.UnitTests.Admin;
public sealed class AdminUserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHashingService> _mockPasswordHashingService;
    private readonly Mock<IAdminAuditLogger> _mockAuditLogger;
    private readonly Mock<ILogger<AdminUserService>> _mockLogger;
    private readonly AdminUserService _service;
    private readonly UserId _testUserId;
    private readonly Guid _adminId;

    public AdminUserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHashingService = new Mock<IPasswordHashingService>();
        _mockAuditLogger = new Mock<IAdminAuditLogger>();
        _mockLogger = new Mock<ILogger<AdminUserService>>();
        _testUserId = UserId.From(Guid.NewGuid());
        _adminId = Guid.NewGuid();

        _service = new AdminUserService(
            _mockUserRepository.Object,
            _mockPasswordHashingService.Object,
            _mockAuditLogger.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task QueryUsersAsync_WithNoFilters_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            User.Create("user1", "user1@example.com", "hash1", UserRole.User),
            User.Create("user2", "user2@example.com", "hash2", UserRole.Admin)
        };

        _mockUserRepository
            .Setup(x => x.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<UserRole?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, users.Count));

        var filter = new AdminUserFilter(null, null, null, null, false);

        // Act
        var result = await _service.QueryUsersAsync(filter, 10, 0);

        // Assert
        result.Users.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithExistingUser_ShouldReturnUserDetail()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hash", UserRole.User);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserByIdAsync(user.Id.Value);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        _mockUserRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetUserByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ActivateUserAsync_WithValidUser_ShouldActivateAndLogAudit()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hash", UserRole.User);
        var deactivated = user.Deactivate();

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(deactivated);

        // Act
        await _service.ActivateUserAsync(user.Id.Value, _adminId);

        // Assert
        _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => u.IsActive)), Times.Once);
        _mockAuditLogger.Verify(
            x => x.LogAsync(
                It.Is<AdminAuditEvent>(e => e.Action == AdminAuditAction.ActivatedUser),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateUserAsync_WithValidUser_ShouldDeactivateAndLogAudit()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hash", UserRole.User);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        await _service.DeactivateUserAsync(user.Id.Value, _adminId);

        // Assert
        _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => !u.IsActive)), Times.Once);
        _mockAuditLogger.Verify(
            x => x.LogAsync(
                It.Is<AdminAuditEvent>(e => e.Action == AdminAuditAction.DeactivatedUser),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_WithValidUser_ShouldChangeRoleAndLogAudit()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hash", UserRole.User);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        await _service.ChangeUserRoleAsync(user.Id.Value, UserRole.Admin, _adminId);

        // Assert
        _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => u.Role == UserRole.Admin)), Times.Once);
        _mockAuditLogger.Verify(
            x => x.LogAsync(
                It.Is<AdminAuditEvent>(e => e.Action == AdminAuditAction.ChangedUserRole),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetUserPasswordAsync_WithValidPassword_ShouldResetPasswordAndLogAudit()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "oldhash", UserRole.User);
        var newPassword = "NewPassword123";
        var newHash = "newhash";

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        _mockPasswordHashingService
            .Setup(x => x.HashPassword(newPassword))
            .Returns(newHash);

        // Act
        await _service.ResetUserPasswordAsync(user.Id.Value, newPassword, _adminId);

        // Assert
        _mockPasswordHashingService.Verify(x => x.HashPassword(newPassword), Times.Once);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockAuditLogger.Verify(
            x => x.LogAsync(
                It.Is<AdminAuditEvent>(e => e.Action == AdminAuditAction.ResetUserPassword),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Short")]
    [InlineData("1234567")]
    public async Task ResetUserPasswordAsync_WithInvalidPassword_ShouldThrowArgumentException(string? invalidPassword)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await _service.ResetUserPasswordAsync(userId, invalidPassword!, _adminId);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("newPassword");
    }

    [Fact]
    public async Task DeleteUserAsync_WithValidUser_ShouldSoftDeleteAndLogAudit()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hash", UserRole.User);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        await _service.DeleteUserAsync(user.Id.Value, _adminId);

        // Assert
        _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => u.IsDeleted)), Times.Once);
        _mockAuditLogger.Verify(
            x => x.LogAsync(
                It.Is<AdminAuditEvent>(e => e.Action == AdminAuditAction.DeletedUser),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ActivateUserAsync_WithNonExistingUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockUserRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var act = async () => await _service.ActivateUserAsync(Guid.NewGuid(), _adminId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found");
    }

    [Fact]
    public void Constructor_WithNullUserRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AdminUserService(
            null!,
            _mockPasswordHashingService.Object,
            _mockAuditLogger.Object,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("userRepository");
    }

    [Fact]
    public void Constructor_WithNullPasswordHashingService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AdminUserService(
            _mockUserRepository.Object,
            null!,
            _mockAuditLogger.Object,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("passwordHashingService");
    }

    [Fact]
    public void Constructor_WithNullAuditLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AdminUserService(
            _mockUserRepository.Object,
            _mockPasswordHashingService.Object,
            null!,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("auditLogger");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AdminUserService(
            _mockUserRepository.Object,
            _mockPasswordHashingService.Object,
            _mockAuditLogger.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}