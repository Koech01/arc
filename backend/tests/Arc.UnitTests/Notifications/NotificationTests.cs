using Arc.Domain.Models;
namespace Arc.UnitTests.Notifications;


/// <summary>
/// Unit tests for Notification domain model.
/// Validates domain invariants and business rules.
/// </summary>
public sealed class NotificationTests
{
    [Fact]
    public void Create_WithValidData_CreatesNotification()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var title = "Test Notification";
        var message = "This is a test message";

        // Act
        var notification = Notification.Create(userId, title, message, NotificationType.Info);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(userId, notification.UserId);
        Assert.Equal(title, notification.Title);
        Assert.Equal(message, notification.Message);
        Assert.Equal(NotificationType.Info, notification.Type);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void Constructor_WithEmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Notification(
                NotificationId.Create(),
                userId,
                "",
                "Message",
                NotificationType.Info,
                false,
                DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithTitleTooLong_ThrowsArgumentException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var longTitle = new string('a', 256);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Notification(
                NotificationId.Create(),
                userId,
                longTitle,
                "Message",
                NotificationType.Info,
                false,
                DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithEmptyMessage_ThrowsArgumentException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Notification(
                NotificationId.Create(),
                userId,
                "Title",
                "",
                NotificationType.Info,
                false,
                DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithMessageTooLong_ThrowsArgumentException()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var longMessage = new string('a', 2001);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Notification(
                NotificationId.Create(),
                userId,
                "Title",
                longMessage,
                NotificationType.Info,
                false,
                DateTime.UtcNow));
    }

    [Fact]
    public void MarkAsRead_WhenUnread_ReturnsNewNotificationWithReadTrue()
    {
        // Arrange
        var notification = Notification.Create(
            UserId.From(Guid.NewGuid()),
            "Title",
            "Message",
            NotificationType.Info);

        // Act
        var readNotification = notification.MarkAsRead();

        // Assert
        Assert.True(readNotification.IsRead);
        Assert.False(notification.IsRead); // Original unchanged
        Assert.Equal(notification.Id, readNotification.Id);
        Assert.Equal(notification.Title, readNotification.Title);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_ReturnsSameInstance()
    {
        // Arrange
        var notification = Notification.Create(
            UserId.From(Guid.NewGuid()),
            "Title",
            "Message",
            NotificationType.Info);
        var readNotification = notification.MarkAsRead();

        // Act
        var result = readNotification.MarkAsRead();

        // Assert
        Assert.Same(readNotification, result);
    }

    [Theory]
    [InlineData(NotificationType.Info, "info")]
    [InlineData(NotificationType.Success, "success")]
    [InlineData(NotificationType.Warning, "warning")]
    [InlineData(NotificationType.Error, "error")]
    public void NotificationType_ToTypeString_ReturnsCorrectString(NotificationType type, string expected)
    {
        // Act
        var result = type.ToTypeString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("info", NotificationType.Info)]
    [InlineData("success", NotificationType.Success)]
    [InlineData("warning", NotificationType.Warning)]
    [InlineData("error", NotificationType.Error)]
    [InlineData("INFO", NotificationType.Info)]
    [InlineData("SUCCESS", NotificationType.Success)]
    public void NotificationType_FromTypeString_ReturnsCorrectType(string typeString, NotificationType expected)
    {
        // Act
        var result = NotificationTypeExtensions.FromTypeString(typeString);

        // Assert
        Assert.Equal(expected, result);
    }
}