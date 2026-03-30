using Arc.Domain.Models;
namespace Arc.Application.Webhooks;


/// <summary>
/// Repository interface for webhook persistence operations.
/// </summary>
public interface IWebhookRepository
{
    Task<Webhook> CreateAsync(Webhook webhook, CancellationToken cancellationToken = default);
    Task<Webhook?> GetByIdAsync(WebhookId webhookId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Webhook>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(WebhookId webhookId, CancellationToken cancellationToken = default);
    Task<bool> UpdateIsActiveAsync(WebhookId webhookId, bool isActive, CancellationToken cancellationToken = default);
    /// <summary>
    /// Replaces the url, events, and secret of an existing webhook.
    /// Returns false if the webhook was not found.
    /// </summary>
    Task<bool> UpdateAsync(Webhook webhook, CancellationToken cancellationToken = default);

    /// <summary>Admin-only: returns all webhooks across all users, ordered by created_at DESC.</summary>
    Task<(IReadOnlyList<Webhook> Webhooks, int TotalCount)> GetAllAsync(
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    /// <summary>Admin-only: deactivates all webhooks for a specific user.</summary>
    Task<int> DeactivateByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
}