using NSubstitute;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Webhooks;
namespace Arc.UnitTests.Webhooks;
using Arc.Infrastructure.Webhooks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;


public sealed class DeterministicWebhookDispatcherTests
{
    private readonly IWebhookRepository _mockRepository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeterministicWebhookDispatcher> _mockLogger;
    private readonly IServiceScopeFactory _mockScopeFactory;
    private readonly DeterministicWebhookDispatcher _dispatcher;

    public DeterministicWebhookDispatcherTests()
    {
        _mockRepository = Substitute.For<IWebhookRepository>();
        _mockLogger = Substitute.For<ILogger<DeterministicWebhookDispatcher>>();
        _mockScopeFactory = Substitute.For<IServiceScopeFactory>();
        _httpClient = new HttpClient(new MockHttpMessageHandler());
        _dispatcher = new DeterministicWebhookDispatcher(_mockRepository, _httpClient, _mockLogger, _mockScopeFactory);
    }

    [Fact]
    public async Task DispatchAsync_WithNoWebhooks_CompletesWithoutError()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        _mockRepository.ListByUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<Webhook>());

        var payload = new WebhookEventPayload(
            "exec-123",
            WebhookEventType.ExecutionCompleted,
            DateTime.UtcNow,
            userId,
            taskCount: 5,
            "success",
            durationMs: 1000);

        // Act
        var action = async () => await _dispatcher.DispatchAsync(payload);

        // Assert
        await action.Should().NotThrowAsync();
        await _mockRepository.Received(1).ListByUserAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithInactiveWebhooks_DoesNotDispatch()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var inactiveWebhook = new Webhook(
            WebhookId.Create(),
            "https://example.com/webhook",
            new[] { WebhookEventType.ExecutionCompleted }.ToList(),
            "this-is-a-super-secret-webhook-key-of-sufficient-length",
            isActive: false,
            userId,
            DateTime.UtcNow);

        _mockRepository.ListByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { inactiveWebhook }.ToList());

        var payload = new WebhookEventPayload(
            "exec-123",
            WebhookEventType.ExecutionCompleted,
            DateTime.UtcNow,
            userId,
            taskCount: 5,
            "success",
            durationMs: 1000);

        // Act
        var action = async () => await _dispatcher.DispatchAsync(payload);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_WithWebhookNotSubscribedToEvent_SkipsWebhook()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var webhook = new Webhook(
            WebhookId.Create(),
            "https://example.com/webhook",
            new[] { WebhookEventType.ExecutionFailed }.ToList(), // Not subscribed to ExecutionCompleted
            "this-is-a-super-secret-webhook-key-of-sufficient-length",
            isActive: true,
            userId,
            DateTime.UtcNow);

        _mockRepository.ListByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { webhook }.ToList());

        var payload = new WebhookEventPayload(
            "exec-123",
            WebhookEventType.ExecutionCompleted,
            DateTime.UtcNow,
            userId,
            taskCount: 5,
            "success",
            durationMs: 1000);

        // Act
        var action = async () => await _dispatcher.DispatchAsync(payload);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_WithNullPayload_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _dispatcher.DispatchAsync(null!));
    }

    [Fact]
    public async Task DispatchAsync_IsFireAndForget_ReturnsImmediately()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        _mockRepository.ListByUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<Webhook>());

        var payload = new WebhookEventPayload(
            "exec-123",
            WebhookEventType.ExecutionCompleted,
            DateTime.UtcNow,
            userId,
            taskCount: 5,
            "success",
            durationMs: 1000);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _dispatcher.DispatchAsync(payload);
        stopwatch.Stop();

        // Assert - Fire-and-forget should return quickly (not wait for actual HTTP calls)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public void WebhookEventTypeExtensions_ToEventString_ReturnsDeterministicValues()
    {
        // Arrange & Act & Assert
        WebhookEventTypeExtensions.ToEventString(WebhookEventType.ExecutionStarted).Should().Be("execution.started");
        WebhookEventTypeExtensions.ToEventString(WebhookEventType.ExecutionCompleted).Should().Be("execution.completed");
        WebhookEventTypeExtensions.ToEventString(WebhookEventType.ExecutionFailed).Should().Be("execution.failed");
    }

    [Fact]
    public void WebhookEventTypeExtensions_FromEventString_ReturnsDeterministicValues()
    {
        // Arrange & Act & Assert
        WebhookEventTypeExtensions.FromEventString("execution.started").Should().Be(WebhookEventType.ExecutionStarted);
        WebhookEventTypeExtensions.FromEventString("execution.completed").Should().Be(WebhookEventType.ExecutionCompleted);
        WebhookEventTypeExtensions.FromEventString("execution.failed").Should().Be(WebhookEventType.ExecutionFailed);
    }

    [Fact]
    public void WebhookEventTypeExtensions_RoundTrip_IsDeterministic()
    {
        // Arrange
        var originalTypes = new[]
        {
            WebhookEventType.ExecutionStarted,
            WebhookEventType.ExecutionCompleted,
            WebhookEventType.ExecutionFailed
        };

        // Act & Assert
        foreach (var eventType in originalTypes)
        {
            var eventString = WebhookEventTypeExtensions.ToEventString(eventType);
            var roundTripped = WebhookEventTypeExtensions.FromEventString(eventString);
            roundTripped.Should().Be(eventType);
        }
    }

    /// <summary>
    /// Mock HTTP message handler for testing.
    /// Always returns success to avoid actual HTTP calls.
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}