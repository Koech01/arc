using System.Text;
using System.Text.Json;
using Arc.Domain.Models;
using Arc.Application.Webhooks;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Arc.Application.Notifications;
namespace Arc.Infrastructure.Webhooks;
using Microsoft.Extensions.DependencyInjection;


/// <summary>
/// Deterministic webhook dispatcher with HMAC-SHA256 signing and exponential backoff retry.
/// Determinism is preserved by using attempt number to seed backoff delays (no randomization).
/// All webhook delivery happens asynchronously without throwing exceptions (fire-and-forget).
/// </summary>
public sealed class DeterministicWebhookDispatcher : IWebhookDispatcher
{
    private readonly IWebhookRepository _repository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeterministicWebhookDispatcher> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _maxRetries;
    private readonly int _initialDelayMs;

    public DeterministicWebhookDispatcher(
        IWebhookRepository repository,
        HttpClient httpClient,
        ILogger<DeterministicWebhookDispatcher> logger,
        IServiceScopeFactory scopeFactory,
        int maxRetries = 3,
        int initialDelayMs = 500)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _maxRetries = maxRetries;
        _initialDelayMs = initialDelayMs;
    }

    public async Task DispatchAsync(WebhookEventPayload payload, CancellationToken cancellationToken = default)
    {
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));

        try
        {
            var webhooks = await _repository.ListByUserAsync(payload.UserId, cancellationToken);
            var activeWebhooks = webhooks.Where(w => w.IsActive && w.IsSubscribedTo(payload.EventType)).ToList();

            if (activeWebhooks.Count == 0)
            {
                _logger.LogDebug("No active webhooks found for event {EventType} user {UserId}", 
                    payload.EventType.ToEventString(), payload.UserId.Value);
                return;
            }

            _logger.LogInformation("Dispatching webhook event {EventType} to {Count} webhooks for user {UserId}",
                payload.EventType.ToEventString(), activeWebhooks.Count, payload.UserId.Value);

            // Fire-and-forget: dispatch all webhooks concurrently without waiting
            _ = Task.Run(async () =>
            {
                var tasks = activeWebhooks.Select(webhook => 
                    DispatchToWebhookAsync(webhook, payload, cancellationToken));
                await Task.WhenAll(tasks);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during webhook dispatch for event {EventType}", payload.EventType.ToEventString());
        }
    }

    private async Task DispatchToWebhookAsync(Webhook webhook, WebhookEventPayload payload, CancellationToken cancellationToken)
    {
        var payloadJson = SerializePayload(payload);
        var signature = ComputeSignature(payloadJson, webhook.Secret);

        int attempt = 0;
        while (attempt < _maxRetries)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
                {
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };

                // Add deterministic HMAC-SHA256 signature header
                request.Headers.Add("X-Webhook-Signature", signature);
                request.Headers.Add("X-Webhook-Event", payload.EventType.ToEventString());
                request.Headers.Add("X-Webhook-Timestamp", payload.Timestamp.ToUniversalTime().ToString("O"));

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10)); // 10 second timeout per request

                var response = await _httpClient.SendAsync(request, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Webhook delivered successfully to {Url} for event {EventType}",
                        webhook.Url, payload.EventType.ToEventString());
                    return;
                }

                if (!IsTransientError(response.StatusCode))
                {
                    _logger.LogWarning("Webhook delivery failed with non-transient error ({StatusCode}) to {Url}",
                        response.StatusCode, webhook.Url);
                    return;
                }

                attempt++;
                if (attempt < _maxRetries)
                {
                    var delay = ComputeDeterministicBackoff(attempt);
                    _logger.LogDebug("Webhook delivery failed with transient error, retrying in {DelayMs}ms (attempt {Attempt}/{MaxRetries})",
                        delay, attempt, _maxRetries);
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Webhook delivery to {Url} timed out (attempt {Attempt}/{MaxRetries})",
                    webhook.Url, attempt + 1, _maxRetries);
                attempt++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error dispatching webhook to {Url} (attempt {Attempt}/{MaxRetries})",
                    webhook.Url, attempt + 1, _maxRetries);
                attempt++;
            }
        }

        _logger.LogError("Webhook delivery to {Url} failed after {MaxRetries} retries for event {EventType}",
            webhook.Url, _maxRetries, payload.EventType.ToEventString());

        // Persist a user-facing Error notification so the permanent delivery failure is
        // visible in the notifications panel. Uses a fresh DI scope because this method
        // runs inside a fire-and-forget Task.Run that may outlive the originating request scope.
        await PersistDeliveryFailureNotificationAsync(
            payload.UserId, webhook.Url, payload.EventType.ToEventString(), _maxRetries);
    }

    /// <summary>
    /// Creates a new DI scope and persists a webhook delivery failure notification.
    /// A dedicated scope is required because this is called from a background Task.Run
    /// that may execute after the HTTP request scope has been disposed.
    /// Exceptions are swallowed and logged to preserve the fire-and-forget contract.
    /// </summary>
    private async Task PersistDeliveryFailureNotificationAsync(
        UserId userId,
        string webhookUrl,
        string eventType,
        int attempts)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notificationService.NotifyWebhookDeliveryFailedAsync(
                userId, webhookUrl, eventType, attempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist webhook delivery failure notification for user {UserId}",
                userId.Value);
        }
    }

    /// <summary>
    /// Computes deterministic exponential backoff: delay = initial × 2^(attempt-1)
    /// Example: initial=500ms, attempt=1 → 500ms, attempt=2 → 1000ms, attempt=3 → 2000ms
    /// No randomization ensures determinism.
    /// </summary>
    private int ComputeDeterministicBackoff(int attempt)
    {
        if (attempt <= 0) return _initialDelayMs;
        return _initialDelayMs * (int)Math.Pow(2, attempt - 1);
    }

    private static bool IsTransientError(System.Net.HttpStatusCode statusCode)
    {
        return statusCode == System.Net.HttpStatusCode.InternalServerError
            || statusCode == System.Net.HttpStatusCode.BadGateway
            || statusCode == System.Net.HttpStatusCode.ServiceUnavailable
            || statusCode == System.Net.HttpStatusCode.GatewayTimeout
            || (int)statusCode == 429; // Too Many Requests
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string SerializePayload(WebhookEventPayload payload)
    {
        var dto = new
        {
            executionId = payload.ExecutionId,
            eventType = payload.EventType.ToEventString(),
            timestamp = payload.Timestamp.ToUniversalTime().ToString("O"),
            userId = payload.UserId.Value,
            taskCount = payload.TaskCount,
            status = payload.Status,
            durationMs = payload.DurationMs,
            errorMessage = payload.ErrorMessage
        };

        return JsonSerializer.Serialize(dto, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}