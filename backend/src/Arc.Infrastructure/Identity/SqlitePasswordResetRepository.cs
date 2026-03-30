using Arc.Domain.Models;
using Microsoft.Data.Sqlite;
using Arc.Application.Identity;
namespace Arc.Infrastructure.Identity;


public sealed class SqlitePasswordResetRepository : IPasswordResetRepository
{
    private readonly string _connectionString;

    public SqlitePasswordResetRepository(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS password_reset_tokens (
                id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                token TEXT UNIQUE NOT NULL,
                expires_at_utc TEXT NOT NULL,
                used INTEGER NOT NULL DEFAULT 0,
                created_at_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_token ON password_reset_tokens(token);
        ";
        command.ExecuteNonQuery();
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, user_id, token, expires_at_utc, used, created_at_utc FROM password_reset_tokens WHERE token = @token";
        command.Parameters.AddWithValue("@token", token);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new PasswordResetToken(
                Guid.Parse(reader.GetString(0)),
                UserId.From(Guid.Parse(reader.GetString(1))),
                reader.GetString(2),
                DateTime.Parse(reader.GetString(3)),
                reader.GetInt32(4) == 1,
                DateTime.Parse(reader.GetString(5))
            );
        }
        return null;
    }

    public async Task CreateAsync(PasswordResetToken resetToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO password_reset_tokens (id, user_id, token, expires_at_utc, used, created_at_utc)
            VALUES (@id, @userId, @token, @expiresAt, @used, @createdAt)";
        command.Parameters.AddWithValue("@id", resetToken.Id.ToString());
        command.Parameters.AddWithValue("@userId", resetToken.UserId.Value.ToString());
        command.Parameters.AddWithValue("@token", resetToken.Token);
        command.Parameters.AddWithValue("@expiresAt", resetToken.ExpiresAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@used", resetToken.Used ? 1 : 0);
        command.Parameters.AddWithValue("@createdAt", resetToken.CreatedAtUtc.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(PasswordResetToken resetToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE password_reset_tokens SET used = @used WHERE id = @id";
        command.Parameters.AddWithValue("@used", resetToken.Used ? 1 : 0);
        command.Parameters.AddWithValue("@id", resetToken.Id.ToString());
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteExpiredTokensAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM password_reset_tokens WHERE expires_at_utc < @now";
        command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }
}