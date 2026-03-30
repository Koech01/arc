using Npgsql;
using Arc.Domain.Models;
using Arc.Application.Identity;
using Arc.Application.Persistence;
namespace Arc.Infrastructure.Identity;


public sealed class PostgresPasswordResetRepository : IPasswordResetRepository
{
    private readonly IDatabaseContext _dbContext;

    public PostgresPasswordResetRepository(IDatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = @"
            SELECT id, user_id, token, expires_at_utc, used, created_at_utc
            FROM password_reset_tokens
            WHERE token = @token";
        
        command.Parameters.AddWithValue("@token", token);
        
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new PasswordResetToken(
                reader.GetGuid(0),
                UserId.From(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetDateTime(3),
                reader.GetBoolean(4),
                reader.GetDateTime(5)
            );
        }
        
        return null;
    }

    public async Task CreateAsync(PasswordResetToken resetToken)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = @"
            INSERT INTO password_reset_tokens (id, user_id, token, expires_at_utc, used, created_at_utc)
            VALUES (@id, @userId, @token, @expiresAt, @used, @createdAt)";
        
        command.Parameters.AddWithValue("@id", resetToken.Id);
        command.Parameters.AddWithValue("@userId", resetToken.UserId.Value);
        command.Parameters.AddWithValue("@token", resetToken.Token);
        command.Parameters.AddWithValue("@expiresAt", resetToken.ExpiresAtUtc);
        command.Parameters.AddWithValue("@used", resetToken.Used);
        command.Parameters.AddWithValue("@createdAt", resetToken.CreatedAtUtc);
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(PasswordResetToken resetToken)
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = @"
            UPDATE password_reset_tokens
            SET used = @used
            WHERE id = @id";
        
        command.Parameters.AddWithValue("@used", resetToken.Used);
        command.Parameters.AddWithValue("@id", resetToken.Id);
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteExpiredTokensAsync()
    {
        await using var connection = await _dbContext.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = "DELETE FROM password_reset_tokens WHERE expires_at_utc < @now";
        command.Parameters.AddWithValue("@now", DateTime.UtcNow);
        
        await command.ExecuteNonQueryAsync();
    }
}
