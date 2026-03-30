using FluentAssertions;
using Arc.Domain.Models;
using Arc.Domain.Exceptions;
namespace Arc.UnitTests.Domain;


public sealed class WebhookTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesWebhookSuccessfully()
    {
        // Arrange
        var webhookId = WebhookId.Create();
        var url = "https://example.com/webhook";
        var events = new[] { WebhookEventType.ExecutionCompleted }.ToList();
        var secret = "this-is-a-super-secret-webhook-key-of-sufficient-length";
        var userId = new UserId(Guid.NewGuid());
        var createdAt = DateTime.UtcNow;

        // Act
        var webhook = new Webhook(webhookId, url, events, secret, isActive: true, userId, createdAt);

        // Assert
        webhook.Id.Should().Be(webhookId);
        webhook.Url.Should().Be(url);
        webhook.Events.Should().Equal(events);
        webhook.Secret.Should().Be(secret);
        webhook.IsActive.Should().BeTrue();
        webhook.CreatedBy.Should().Be(userId);
        webhook.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Constructor_WithEmptyUrl_ThrowsWebhookException()
    {
        // Arrange
        var webhookId = WebhookId.Create();
        var userId = new UserId(Guid.NewGuid());
        var events = new[] { WebhookEventType.ExecutionCompleted }.ToList();
        var secret = "this-is-a-super-secret-webhook-key-of-sufficient-length";

        // Act & Assert
        var ex = Assert.Throws<WebhookException>(() =>
            new Webhook(webhookId, "", events, secret, isActive: true, userId, DateTime.UtcNow));
        ex.Message.Should().Contain("URL cannot be empty");
    }

    [Fact]
    public void Constructor_WithInvalidUrl_ThrowsWebhookException()
    {
        // Arrange
        var webhookId = WebhookId.Create();
        var userId = new UserId(Guid.NewGuid());
        var events = new[] { WebhookEventType.ExecutionCompleted }.ToList();
        var secret = "this-is-a-super-secret-webhook-key-of-sufficient-length";

        // Act & Assert
        var ex = Assert.Throws<WebhookException>(() =>
            new Webhook(webhookId, "not-a-valid-url", events, secret, isActive: true, userId, DateTime.UtcNow));
        ex.Message.Should().Contain("Invalid webhook URL");
    }

    [Fact]
    public void Constructor_WithFtpUrl_ThrowsWebhookException()
    {
        // Arrange
        var webhookId = WebhookId.Create();
        var userId = new UserId(Guid.NewGuid());
        var events = new[] { WebhookEventType.ExecutionCompleted }.ToList();
        var secret = "this-is-a-super-secret-webhook-key-of-sufficient-length";

        // Act & Assert
        var ex = Assert.Throws<WebhookException>(() =>
            new Webhook(webhookId, "ftp://example.com/webhook", events, secret, isActive: true, userId, DateTime.UtcNow));
        ex.Message.Should().Contain("Invalid webhook URL");
    }

    [Fact]
    public void Constructor_WithEmptyEvents_ThrowsWebhookException()
    {
        // Arrange
        var webhookId = WebhookId.Create();
        var url = "https://example.com/webhook";
        var events = new List<WebhookEventType>();
        var secret = "this-is-a-super-secret-webhook-key-of-sufficient-length";
        var userId = new UserId(Guid.NewGuid());

        // Act & Assert
        var ex = Assert.Throws<WebhookException>(() =>
            new Webhook(webhookId, url, events, secret, isActive: true, userId, DateTime.UtcNow));
        ex.Message.Should().Contain("must subscribe to at least one event");
    }

    [Fact]
    public void Constructor_WithShortSecret_ThrowsWebhookException()
    {
        // Arrange
        var webhookId = WebhookId.Create();
        var url = "https://example.com/webhook";
        var events = new[] { WebhookEventType.ExecutionCompleted }.ToList();
        var secret = "short"; // Too short
        var userId = new UserId(Guid.NewGuid());

        // Act & Assert
        var ex = Assert.Throws<WebhookException>(() =>
            new Webhook(webhookId, url, events, secret, isActive: true, userId, DateTime.UtcNow));
        ex.Message.Should().Contain("at least 20 characters");
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsWebhookException()
    {
        // Arrange
        var webhookId = WebhookId.Create();
        var url = "https://example.com/webhook";
        var events = new[] { WebhookEventType.ExecutionCompleted }.ToList();
        var secret = "this-is-a-super-secret-webhook-key-of-sufficient-length";
        var userId = new UserId(Guid.Empty);

        // Act & Assert
        var ex = Assert.Throws<WebhookException>(() =>
            new Webhook(webhookId, url, events, secret, isActive: true, userId, DateTime.UtcNow));
        ex.Message.Should().Contain("must have a creator");
    }

    [Fact]
    public void IsSubscribedTo_WithMatchingEvent_ReturnsTrue()
    {
        // Arrange
        var webhookId = WebhookId.Create();
        var url = "https://example.com/webhook";
        var events = new[] { WebhookEventType.ExecutionCompleted, WebhookEventType.ExecutionFailed }.ToList();
        var secret = "this-is-a-super-secret-webhook-key-of-sufficient-length";
        var userId = new UserId(Guid.NewGuid());

        var webhook = new Webhook(webhookId, url, events, secret, isActive: true, userId, DateTime.UtcNow);

        // Act & Assert
        webhook.IsSubscribedTo(WebhookEventType.ExecutionCompleted).Should().BeTrue();
        webhook.IsSubscribedTo(WebhookEventType.ExecutionFailed).Should().BeTrue();
        webhook.IsSubscribedTo(WebhookEventType.ExecutionStarted).Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithMultipleEventTypes_StoresAllEvents()
    {
        // Arrange
        var webhookId = WebhookId.Create();
        var url = "https://example.com/webhook";
        var events = new[] 
        { 
            WebhookEventType.ExecutionStarted,
            WebhookEventType.ExecutionCompleted,
            WebhookEventType.ExecutionFailed
        }.ToList();
        var secret = "this-is-a-super-secret-webhook-key-of-sufficient-length";
        var userId = new UserId(Guid.NewGuid());

        // Act
        var webhook = new Webhook(webhookId, url, events, secret, isActive: true, userId, DateTime.UtcNow);

        // Assert
        webhook.Events.Should().HaveCount(3);
        webhook.Events.Should().Contain(WebhookEventType.ExecutionStarted);
        webhook.Events.Should().Contain(WebhookEventType.ExecutionCompleted);
        webhook.Events.Should().Contain(WebhookEventType.ExecutionFailed);
    }

    [Fact]
    public void Constructor_WithInactiveStatus_CreatesInactiveWebhook()
    {
        // Arrange
        var webhookId = WebhookId.Create();
        var url = "https://example.com/webhook";
        var events = new[] { WebhookEventType.ExecutionCompleted }.ToList();
        var secret = "this-is-a-super-secret-webhook-key-of-sufficient-length";
        var userId = new UserId(Guid.NewGuid());

        // Act
        var webhook = new Webhook(webhookId, url, events, secret, isActive: false, userId, DateTime.UtcNow);

        // Assert
        webhook.IsActive.Should().BeFalse();
    }
}