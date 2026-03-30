using Npgsql;
using Arc.Domain.Models;
using Arc.Application.Identity;
using Arc.Application.Persistence;
namespace Arc.Infrastructure.Identity;


/// <summary>
/// PostgreSQL implementation of user preferences repository.
/// Uses upsert pattern for atomic create-or-update operations.
/// </summary>
public sealed class PostgresUserPreferencesRepository : IUserPreferencesRepository
{
    private readonly IDatabaseContext _dbContext;

    public PostgresUserPreferencesRepository(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<UserPreferences?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT user_id, theme, notification_email, notification_push, 
                   notification_execution_complete, notification_execution_failed, 
                   language, timezone
            FROM user_preferences
            WHERE user_id = @user_id";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("user_id", userId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapToUserPreferences(reader);
    }

    public async Task<UserPreferences> UpsertAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO user_preferences 
                (user_id, theme, notification_email, notification_push, 
                 notification_execution_complete, notification_execution_failed, 
                 language, timezone)
            VALUES 
                (@user_id, @theme, @notification_email, @notification_push, 
                 @notification_execution_complete, @notification_execution_failed, 
                 @language, @timezone)
            ON CONFLICT (user_id) 
            DO UPDATE SET
                theme = EXCLUDED.theme,
                notification_email = EXCLUDED.notification_email,
                notification_push = EXCLUDED.notification_push,
                notification_execution_complete = EXCLUDED.notification_execution_complete,
                notification_execution_failed = EXCLUDED.notification_execution_failed,
                language = EXCLUDED.language,
                timezone = EXCLUDED.timezone";

        await using var conn = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("user_id", preferences.UserId.Value);
        cmd.Parameters.AddWithValue("theme", preferences.Theme);
        cmd.Parameters.AddWithValue("notification_email", preferences.NotificationEmail);
        cmd.Parameters.AddWithValue("notification_push", preferences.NotificationPush);
        cmd.Parameters.AddWithValue("notification_execution_complete", preferences.NotificationExecutionComplete);
        cmd.Parameters.AddWithValue("notification_execution_failed", preferences.NotificationExecutionFailed);
        cmd.Parameters.AddWithValue("language", preferences.Language);
        cmd.Parameters.AddWithValue("timezone", preferences.Timezone);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return preferences;
    }

    private static UserPreferences MapToUserPreferences(NpgsqlDataReader reader)
    {
        var userId = (Guid)reader["user_id"];
        var theme = (string)reader["theme"];
        var notificationEmail = (bool)reader["notification_email"];
        var notificationPush = (bool)reader["notification_push"];
        var notificationExecutionComplete = (bool)reader["notification_execution_complete"];
        var notificationExecutionFailed = (bool)reader["notification_execution_failed"];
        var language = (string)reader["language"];
        var timezone = (string)reader["timezone"];

        return new UserPreferences(
            new UserId(userId),
            theme,
            notificationEmail,
            notificationPush,
            notificationExecutionComplete,
            notificationExecutionFailed,
            language,
            timezone);
    }
}