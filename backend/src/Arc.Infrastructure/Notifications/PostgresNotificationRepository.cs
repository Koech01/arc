using Npgsql;
using Arc.Domain.Models;
using Arc.Application.Persistence;
using Arc.Application.Notifications;
namespace Arc.Infrastructure.Notifications;


/// <summary>
/// PostgreSQL implementation of notification repository.
/// Persists notifications with deterministic ordering and user isolation.
/// </summary>
public sealed class PostgresNotificationRepository : INotificationRepository
{
    private readonly IDatabaseContext _dbContext;

    public PostgresNotificationRepository(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO notifications (id, user_id, title, message, type, is_read, created_at)
            VALUES (@id, @user_id, @title, @message, @type, @is_read, @created_at)";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", notification.Id.Value);
        cmd.Parameters.AddWithValue("user_id", notification.UserId.Value);
        cmd.Parameters.AddWithValue("title", notification.Title);
        cmd.Parameters.AddWithValue("message", notification.Message);
        cmd.Parameters.AddWithValue("type", notification.Type.ToTypeString());
        cmd.Parameters.AddWithValue("is_read", notification.IsRead);
        cmd.Parameters.AddWithValue("created_at", notification.CreatedAt);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return notification;
    }

    public async Task<Notification?> GetByIdAsync(NotificationId notificationId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, user_id, title, message, type, is_read, created_at
            FROM notifications
            WHERE id = @id";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", notificationId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapToNotification(reader);
    }

    public async Task<IReadOnlyList<Notification>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, user_id, title, message, type, is_read, created_at
            FROM notifications
            WHERE user_id = @user_id
            ORDER BY created_at DESC";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("user_id", userId.Value);

        var notifications = new List<Notification>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            notifications.Add(MapToNotification(reader));
        }

        return notifications.AsReadOnly();
    }

    public async Task<Notification> UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE notifications
            SET is_read = @is_read
            WHERE id = @id";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", notification.Id.Value);
        cmd.Parameters.AddWithValue("is_read", notification.IsRead);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return notification;
    }

    public async Task<int> MarkAllAsReadAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE notifications
            SET is_read = true
            WHERE user_id = @user_id AND is_read = false";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("user_id", userId.Value);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected;
    }

    public async Task DeleteAsync(NotificationId notificationId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM notifications
            WHERE id = @id";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", notificationId.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Notification MapToNotification(NpgsqlDataReader reader)
    {
        var id = (Guid)reader["id"];
        var userId = (Guid)reader["user_id"];
        var title = (string)reader["title"];
        var message = (string)reader["message"];
        var typeString = (string)reader["type"];
        var isRead = (bool)reader["is_read"];
        var createdAt = (DateTime)reader["created_at"];

        var type = NotificationTypeExtensions.FromTypeString(typeString);

        return new Notification(
            NotificationId.From(id),
            new UserId(userId),
            title,
            message,
            type,
            isRead,
            createdAt);
    }
}