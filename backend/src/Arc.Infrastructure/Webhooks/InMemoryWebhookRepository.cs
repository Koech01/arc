using Arc.Domain.Models;
using System.Collections.Concurrent;
using Arc.Application.Webhooks;
namespace Arc.Infrastructure.Webhooks;


/// <summary>
/// In-memory webhook repository for SQLite/development fallback.
/// </summary>
public sealed class InMemoryWebhookRepository : IWebhookRepository
{
    private readonly ConcurrentDictionary<Guid, Webhook> _store = new();

    public Task<Webhook> CreateAsync(Webhook webhook, CancellationToken cancellationToken = default)
    {
        _store[webhook.Id.Value] = webhook;
        return Task.FromResult(webhook);
    }

    public Task<Webhook?> GetByIdAsync(WebhookId webhookId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(webhookId.Value, out var webhook);
        return Task.FromResult(webhook);
    }

    public Task<IReadOnlyList<Webhook>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var webhooks = _store.Values
            .Where(w => w.CreatedBy == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<Webhook>>(webhooks);
    }

    public Task<bool> DeleteAsync(WebhookId webhookId, CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove(webhookId.Value, out _);
        return Task.FromResult(removed);
    }

    public Task<bool> UpdateIsActiveAsync(WebhookId webhookId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(webhookId.Value, out var w))
            return Task.FromResult(false);

        _store[webhookId.Value] = new Webhook(w.Id, w.Url, w.Events, w.Secret, isActive, w.CreatedBy, w.CreatedAt);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateAsync(Webhook webhook, CancellationToken cancellationToken = default)
    {
        if (!_store.ContainsKey(webhook.Id.Value))
            return Task.FromResult(false);

        _store[webhook.Id.Value] = webhook;
        return Task.FromResult(true);
    }

    public Task<(IReadOnlyList<Webhook> Webhooks, int TotalCount)> GetAllAsync(
        int limit, int offset, CancellationToken cancellationToken = default)
    {
        var all = _store.Values.OrderByDescending(w => w.CreatedAt).ToList();
        var page = all.Skip(offset).Take(limit).ToList();
        return Task.FromResult<(IReadOnlyList<Webhook>, int)>((page, all.Count));
    }

    public Task<int> DeactivateByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var key in _store.Keys.ToList())
        {
            if (_store.TryGetValue(key, out var w) && w.CreatedBy == userId && w.IsActive)
            {
                _store[key] = new Webhook(w.Id, w.Url, w.Events, w.Secret, false, w.CreatedBy, w.CreatedAt);
                count++;
            }
        }
        return Task.FromResult(count);
    }
}
