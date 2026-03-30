using Moq;
using FluentAssertions;
namespace Arc.UnitTests.Admin;
using Arc.Infrastructure.Admin;
using Microsoft.Extensions.Logging;


public sealed class InMemoryMaintenanceModeServiceTests
{
    private readonly Mock<ILogger<InMemoryMaintenanceModeService>> _mockLogger;
    private readonly InMemoryMaintenanceModeService _service;
    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public InMemoryMaintenanceModeServiceTests()
    {
        _mockLogger = new Mock<ILogger<InMemoryMaintenanceModeService>>();
        _service = new InMemoryMaintenanceModeService();
    }

    [Fact]
    public void IsMaintenanceModeEnabled_Initially_ShouldReturnFalse()
    {
        // Act
        var result = _service.IsEnabled;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnableMaintenanceMode_ShouldSetMaintenanceModeToTrue()
    {
        // Act
        _service.Enable(_adminId, null);

        // Assert
        _service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void DisableMaintenanceMode_ShouldSetMaintenanceModeToFalse()
    {
        // Arrange
        _service.Enable(_adminId, null);

        // Act
        _service.Disable(_adminId);

        // Assert
        _service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void EnableMaintenanceMode_CalledMultipleTimes_ShouldRemainEnabled()
    {
        // Act
        _service.Enable(_adminId, null);
        _service.Enable(_adminId, null);
        _service.Enable(_adminId, null);

        // Assert
        _service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void DisableMaintenanceMode_CalledMultipleTimes_ShouldRemainDisabled()
    {
        // Arrange
        _service.Enable(_adminId, null);

        // Act
        _service.Disable(_adminId);
        _service.Disable(_adminId);
        _service.Disable(_adminId);

        // Assert
        _service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Toggle_ShouldChangeMaintenanceModeState()
    {
        // Arrange
        var initialState = _service.IsEnabled;

        // Act
        _service.Enable(_adminId, null);
        var afterEnable = _service.IsEnabled;
        _service.Disable(_adminId);
        var afterDisable = _service.IsEnabled;

        // Assert
        initialState.Should().BeFalse();
        afterEnable.Should().BeTrue();
        afterDisable.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateService()
    {
        // Act
        var service = new InMemoryMaintenanceModeService();

        // Assert
        service.Should().NotBeNull();
        service.IsEnabled.Should().BeFalse();
    }
}