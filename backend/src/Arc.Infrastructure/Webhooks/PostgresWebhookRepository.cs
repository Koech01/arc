using Npgsql;
using System.Text.Json;
using Arc.Domain.Models;
using Arc.Application.Webhooks;
using Arc.Application.Persistence;
namespace Arc.Infrastructure.Webhooks;


/// <summary>
/// PostgreSQL implementation of webhook repository.
/// Persists webhooks with JSON serialization of event subscriptions.
/// </summary>
/// 
public sealed class PostgresWebhookRepository : IWebhookRepository
{
    private readonly IDatabaseContext _dbContext;

    public PostgresWebhookRepository(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Webhook> CreateAsync(Webhook webhook, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO webhooks (id, url, events_json, secret, is_active, created_by, created_at)
            VALUES (@id, @url, @events_json, @secret, @is_active, @created_by, @created_at)
            RETURNING id";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", webhook.Id.Value);
        cmd.Parameters.AddWithValue("url", webhook.Url);
        cmd.Parameters.AddWithValue("events_json", SerializeEvents(webhook.Events));
        cmd.Parameters.AddWithValue("secret", webhook.Secret);
        cmd.Parameters.AddWithValue("is_active", webhook.IsActive);
        cmd.Parameters.AddWithValue("created_by", webhook.CreatedBy.Value);
        cmd.Parameters.AddWithValue("created_at", webhook.CreatedAt);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Failed to insert webhook into database");
            
        return webhook;
    }

    public async Task<Webhook?> GetByIdAsync(WebhookId webhookId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, url, events_json, secret, is_active, created_by, created_at
            FROM webhooks
            WHERE id = @id";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", webhookId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapToWebhook(reader);
    }

    public async Task<IReadOnlyList<Webhook>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, url, events_json, secret, is_active, created_by, created_at
            FROM webhooks
            WHERE created_by = @created_by
            ORDER BY created_at DESC";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("created_by", userId.Value);

        var webhooks = new List<Webhook>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            webhooks.Add(MapToWebhook(reader));
        }

        return webhooks.AsReadOnly();
    }

    public async Task<bool> DeleteAsync(WebhookId webhookId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM webhooks WHERE id = @id";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", webhookId.Value);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateIsActiveAsync(WebhookId webhookId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE webhooks SET is_active = @is_active WHERE id = @id";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", webhookId.Value);
        cmd.Parameters.AddWithValue("is_active", isActive);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    /// <summary>
    /// Updates the url, events, and secret of an existing webhook.
    /// is_active, created_by, and created_at are preserved unchanged.
    /// </summary>
    public async Task<bool> UpdateAsync(Webhook webhook, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE webhooks
            SET url = @url, events_json = @events_json, secret = @secret
            WHERE id = @id";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", webhook.Id.Value);
        cmd.Parameters.AddWithValue("url", webhook.Url);
        cmd.Parameters.AddWithValue("events_json", SerializeEvents(webhook.Events));
        cmd.Parameters.AddWithValue("secret", webhook.Secret);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    /// <summary>Admin-only: returns all webhooks across all users, ordered by created_at DESC.</summary>
    public async Task<(IReadOnlyList<Webhook> Webhooks, int TotalCount)> GetAllAsync(
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);

        await using var countCmd = new NpgsqlCommand("SELECT COUNT(1) FROM webhooks", conn);
        var total = (int)Convert.ToInt64(await countCmd.ExecuteScalarAsync(cancellationToken));

        await using var cmd = new NpgsqlCommand(@"
            SELECT id, url, events_json, secret, is_active, created_by, created_at
            FROM webhooks
            ORDER BY created_at DESC
            LIMIT @limit OFFSET @offset", conn);
        cmd.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 200));
        cmd.Parameters.AddWithValue("offset", Math.Max(0, offset));

        var webhooks = new List<Webhook>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            webhooks.Add(MapToWebhook(reader));

        return (webhooks.AsReadOnly(), total);
    }

    /// <summary>Admin-only: deactivates all webhooks for a specific user.</summary>
    public async Task<int> DeactivateByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            "UPDATE webhooks SET is_active = false WHERE created_by = @userId AND is_active = true", conn);
        cmd.Parameters.AddWithValue("userId", userId.Value);
        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Webhook MapToWebhook(NpgsqlDataReader reader)
    {
        var id = (Guid)reader["id"];
        var url = (string)reader["url"];
        var eventsJson = (string)reader["events_json"];
        var secret = (string)reader["secret"];
        var isActive = (bool)reader["is_active"];
        var createdBy = (Guid)reader["created_by"];
        var createdAt = (DateTime)reader["created_at"];

        var events = DeserializeEvents(eventsJson);

        return new Webhook(
            WebhookId.From(id),
            url,
            events,
            secret,
            isActive,
            new UserId(createdBy),
            createdAt);
    }

    private static string SerializeEvents(IReadOnlyList<WebhookEventType> events)
    {
        return JsonSerializer.Serialize(events.Select(e => e.ToEventString()).ToList());
    }

    private static IReadOnlyList<WebhookEventType> DeserializeEvents(string json)
    {
        var eventStrings = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        return eventStrings.Select(WebhookEventTypeExtensions.FromEventString).ToList().AsReadOnly();
    }
}