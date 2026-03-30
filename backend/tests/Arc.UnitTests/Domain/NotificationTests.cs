using FluentAssertions;
using Arc.Domain.Models;
namespace Arc.UnitTests.Domain;


public sealed class NotificationTests
{
    private readonly UserId _testUserId = new(Guid.NewGuid());

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateNotification()
    {
        var notificationId = NotificationId.Create();
        var createdAt = DateTime.UtcNow;

        var notification = new Notification(
            notificationId,
            _testUserId,
            "Test Title",
            "Test Message",
            NotificationType.ExecutionCompleted,
            false,
            createdAt);

        notification.Id.Should().Be(notificationId);
        notification.UserId.Should().Be(_testUserId);
        notification.Title.Should().Be("Test Title");
        notification.Message.Should().Be("Test Message");
        notification.Type.Should().Be(NotificationType.ExecutionCompleted);
        notification.IsRead.Should().BeFalse();
        notification.CreatedAt.Should().Be(createdAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyTitle_ShouldThrowException(string title)
    {
        var notificationId = NotificationId.Create();

        var act = () => new Notification(
            notificationId,
            _testUserId,
            title,
            "Test Message",
            NotificationType.ExecutionCompleted,
            false,
            DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("title");
    }

    [Fact]
    public void Constructor_WithTitleTooLong_ShouldThrowException()
    {
        var notificationId = NotificationId.Create();

        var act = () => new Notification(
            notificationId,
            _testUserId,
            new string('a', 256),
            "Test Message",
            NotificationType.ExecutionCompleted,
            false,
            DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("title")
           .WithMessage("*cannot exceed 255 characters*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyMessage_ShouldThrowException(string message)
    {
        var notificationId = NotificationId.Create();

        var act = () => new Notification(
            notificationId,
            _testUserId,
            "Test Title",
            message,
            NotificationType.ExecutionCompleted,
            false,
            DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("message");
    }

    [Fact]
    public void Constructor_WithMessageTooLong_ShouldThrowException()
    {
        var notificationId = NotificationId.Create();

        var act = () => new Notification(
            notificationId,
            _testUserId,
            "Test Title",
            new string('a', 2001),
            NotificationType.ExecutionCompleted,
            false,
            DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("message")
           .WithMessage("*cannot exceed 2000 characters*");
    }

    [Fact]
    public void Constructor_WithNullId_ShouldThrowException()
    {
        var act = () => new Notification(
            null!,
            _testUserId,
            "Test Title",
            "Test Message",
            NotificationType.ExecutionCompleted,
            false,
            DateTime.UtcNow);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ShouldCreateUnreadNotificationWithCurrentTime()
    {
        var before = DateTime.UtcNow;
        
        var notification = Notification.Create(
            _testUserId,
            "Test Title",
            "Test Message",
            NotificationType.ExecutionCompleted);

        var after = DateTime.UtcNow;

        notification.Id.Should().NotBeNull();
        notification.UserId.Should().Be(_testUserId);
        notification.Title.Should().Be("Test Title");
        notification.Message.Should().Be("Test Message");
        notification.Type.Should().Be(NotificationType.ExecutionCompleted);
        notification.IsRead.Should().BeFalse();
        notification.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void MarkAsRead_WhenNotRead_ShouldReturnNewReadNotification()
    {
        var notification = Notification.Create(
            _testUserId,
            "Test Title",
            "Test Message",
            NotificationType.ExecutionCompleted);

        var readNotification = notification.MarkAsRead();

        readNotification.IsRead.Should().BeTrue();
        readNotification.Id.Should().Be(notification.Id);
        readNotification.UserId.Should().Be(notification.UserId);
        readNotification.Title.Should().Be(notification.Title);
        readNotification.Message.Should().Be(notification.Message);
        readNotification.Type.Should().Be(notification.Type);
        readNotification.CreatedAt.Should().Be(notification.CreatedAt);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_ShouldReturnSameInstance()
    {
        var notification = Notification.Create(
            _testUserId,
            "Test Title",
            "Test Message",
            NotificationType.ExecutionCompleted);
        var readNotification = notification.MarkAsRead();

        var result = readNotification.MarkAsRead();

        result.Should().BeSameAs(readNotification);
    }

    [Fact]
    public void Constructor_WithAllNotificationTypes_ShouldSucceed()
    {
        var notificationId = NotificationId.Create();
        var createdAt = DateTime.UtcNow;

        var n1 = new Notification(notificationId, _testUserId, "Title", "Message", NotificationType.ExecutionCompleted, false, createdAt);
        var n2 = new Notification(notificationId, _testUserId, "Title", "Message", NotificationType.ExecutionFailed, false, createdAt);
        var n3 = new Notification(notificationId, _testUserId, "Title", "Message", NotificationType.WebhookDeliveryFailed, false, createdAt);
        var n4 = new Notification(notificationId, _testUserId, "Title", "Message", NotificationType.SystemUpdate, false, createdAt);

        n1.Type.Should().Be(NotificationType.ExecutionCompleted);
        n2.Type.Should().Be(NotificationType.ExecutionFailed);
        n3.Type.Should().Be(NotificationType.WebhookDeliveryFailed);
        n4.Type.Should().Be(NotificationType.SystemUpdate);
    }
}

public sealed class NotificationIdTests
{
    [Fact]
    public void Create_ShouldGenerateNewId()
    {
        var id = NotificationId.Create();

        id.Should().NotBeNull();
        id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        var id1 = NotificationId.Create();
        var id2 = NotificationId.Create();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void Constructor_WithGuid_ShouldCreateId()
    {
        var guid = Guid.NewGuid();

        var id = new NotificationId(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void TwoIds_WithSameGuid_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = new NotificationId(guid);
        var id2 = new NotificationId(guid);

        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void TwoIds_WithDifferentGuids_ShouldNotBeEqual()
    {
        var id1 = new NotificationId(Guid.NewGuid());
        var id2 = new NotificationId(Guid.NewGuid());

        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }
}