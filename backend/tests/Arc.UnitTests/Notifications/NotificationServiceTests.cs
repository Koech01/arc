using NSubstitute;
using FluentAssertions;
using Arc.Domain.Models;
using Microsoft.Extensions.Logging;
using Arc.Application.Notifications;
namespace Arc.UnitTests.Notifications;
using Arc.Infrastructure.Notifications;


public sealed class NotificationServiceTests
{
    [Fact]
    public async Task CreateNotificationAsync_CreatesNotificationWithCorrectProperties()
    {
        // Arrange
        var repository = Substitute.For<INotificationRepository>();
        var logger = Substitute.For<ILogger<DeterministicNotificationService>>();
        var service = new DeterministicNotificationService(repository, logger);

        var userId = new UserId(Guid.NewGuid());
        var title = "Test Notification";
        var message = "This is a test message";
        var type = NotificationType.Info;

        Notification? capturedNotification = null;
        repository.CreateAsync(Arg.Do<Notification>(n => capturedNotification = n), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Notification>());

        // Act
        await service.CreateNotificationAsync(userId, title, message, type);

        // Assert
        await repository.Received(1).CreateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        capturedNotification.Should().NotBeNull();
        capturedNotification!.UserId.Should().Be(userId);
        capturedNotification.Title.Should().Be(title);
        capturedNotification.Message.Should().Be(message);
        capturedNotification.Type.Should().Be(type);
        capturedNotification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task NotifyExecutionStartedAsync_CreatesInfoNotification()
    {
        // Arrange
        var repository = Substitute.For<INotificationRepository>();
        var logger = Substitute.For<ILogger<DeterministicNotificationService>>();
        var service = new DeterministicNotificationService(repository, logger);

        var userId = new UserId(Guid.NewGuid());
        var executionId = "abc123";
        var taskCount = 5;

        Notification? capturedNotification = null;
        repository.CreateAsync(Arg.Do<Notification>(n => capturedNotification = n), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Notification>());

        // Act
        await service.NotifyExecutionStartedAsync(userId, executionId, taskCount);

        // Assert
        await repository.Received(1).CreateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Type.Should().Be(NotificationType.Info);
        capturedNotification.Title.Should().Be("Execution Started");
        capturedNotification.Message.Should().Contain(executionId);
        capturedNotification.Message.Should().Contain(taskCount.ToString());
    }

    [Fact]
    public async Task NotifyExecutionCompletedAsync_CreatesSuccessNotification()
    {
        // Arrange
        var repository = Substitute.For<INotificationRepository>();
        var logger = Substitute.For<ILogger<DeterministicNotificationService>>();
        var service = new DeterministicNotificationService(repository, logger);

        var userId = new UserId(Guid.NewGuid());
        var executionId = "def456";
        var taskCount = 3;
        var durationMs = 2340L;

        Notification? capturedNotification = null;
        repository.CreateAsync(Arg.Do<Notification>(n => capturedNotification = n), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Notification>());

        // Act
        await service.NotifyExecutionCompletedAsync(userId, executionId, taskCount, durationMs);

        // Assert
        await repository.Received(1).CreateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Type.Should().Be(NotificationType.Success);
        capturedNotification.Title.Should().Be("Execution Completed");
        capturedNotification.Message.Should().Contain(executionId);
        capturedNotification.Message.Should().Contain(taskCount.ToString());
        capturedNotification.Message.Should().Contain("2.34s");
    }

    [Fact]
    public async Task NotifyExecutionFailedAsync_CreatesErrorNotification()
    {
        // Arrange
        var repository = Substitute.For<INotificationRepository>();
        var logger = Substitute.For<ILogger<DeterministicNotificationService>>();
        var service = new DeterministicNotificationService(repository, logger);

        var userId = new UserId(Guid.NewGuid());
        var executionId = "ghi789";
        var errorMessage = "Task timeout exceeded";

        Notification? capturedNotification = null;
        repository.CreateAsync(Arg.Do<Notification>(n => capturedNotification = n), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Notification>());

        // Act
        await service.NotifyExecutionFailedAsync(userId, executionId, errorMessage);

        // Assert
        await repository.Received(1).CreateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Type.Should().Be(NotificationType.Error);
        capturedNotification.Title.Should().Be("Execution Failed");
        capturedNotification.Message.Should().Contain(executionId);
        capturedNotification.Message.Should().Contain(errorMessage);
    }

    [Fact]
    public async Task CreateNotificationAsync_HandlesRepositoryException_DoesNotThrow()
    {
        // Arrange
        var repository = Substitute.For<INotificationRepository>();
        var logger = Substitute.For<ILogger<DeterministicNotificationService>>();
        var service = new DeterministicNotificationService(repository, logger);

        var userId = new UserId(Guid.NewGuid());
        repository.CreateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns<Task<Notification>>(_ => throw new Exception("Database error"));

        // Act & Assert - should not throw (fire-and-forget pattern)
        await service.CreateNotificationAsync(userId, "Title", "Message", NotificationType.Info);
        
        // If we reach here, the test passes (no exception thrown)
    }

    [Fact]
    public async Task NotifyExecutionStartedAsync_DeterministicMessage_SameInputProducesSameMessage()
    {
        // Arrange
        var repository = Substitute.For<INotificationRepository>();
        var logger = Substitute.For<ILogger<DeterministicNotificationService>>();
        var service = new DeterministicNotificationService(repository, logger);

        var userId = new UserId(Guid.NewGuid());
        var executionId = "test123";
        var taskCount = 7;

        Notification? firstNotification = null;
        Notification? secondNotification = null;

        repository.CreateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Notification>());

        // Act
        repository.CreateAsync(Arg.Do<Notification>(n => firstNotification = n), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Notification>());
        await service.NotifyExecutionStartedAsync(userId, executionId, taskCount);

        repository.CreateAsync(Arg.Do<Notification>(n => secondNotification = n), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Notification>());
        await service.NotifyExecutionStartedAsync(userId, executionId, taskCount);

        // Assert - same input produces same message (deterministic)
        firstNotification.Should().NotBeNull();
        secondNotification.Should().NotBeNull();
        firstNotification!.Message.Should().Be(secondNotification!.Message);
        firstNotification.Title.Should().Be(secondNotification.Title);
        firstNotification.Type.Should().Be(secondNotification.Type);
    }
}