using Npgsql;
using Arc.Domain.Models;
using Arc.Application.Identity;
using Arc.Application.Persistence;
namespace Arc.Infrastructure.Persistence;


/// <summary>
/// PostgreSQL implementation of user repository.
/// Extended with failed_login_attempts, locked_until_utc, deleted_at,
/// and admin GetAllAsync with filtering and pagination.
/// </summary>
public sealed class PostgresUserRepository : IUserRepository
{
    private const string SelectColumns = @"
        id, username, email, password_hash, role, created_at_utc, is_active, firstname,
        failed_login_attempts, locked_until_utc, deleted_at";

    private readonly IDatabaseContext _dbContext;

    public PostgresUserRepository(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<User?> GetByIdAsync(UserId userId)
    {
        await using var conn = await _dbContext.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            $"SELECT {SelectColumns} FROM users WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", userId.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapUser(reader) : null;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        await using var conn = await _dbContext.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            $"SELECT {SelectColumns} FROM users WHERE email = @email", conn);
        cmd.Parameters.AddWithValue("email", email);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapUser(reader) : null;
    }

    public async Task<User> CreateAsync(User user)
    {
        await using var conn = await _dbContext.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO users
                (id, username, email, password_hash, role, created_at_utc, updated_at_utc,
                 is_active, firstname, failed_login_attempts, locked_until_utc, deleted_at)
            VALUES
                (@id, @username, @email, @passwordHash, @role, @createdAt, @createdAt,
                 @isActive, @firstname, @failedAttempts, @lockedUntil, @deletedAt)",
            conn);

        BindParams(cmd, user);
        await cmd.ExecuteNonQueryAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        await using var conn = await _dbContext.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(@"
            UPDATE users
            SET username              = @username,
                email                 = @email,
                password_hash         = @passwordHash,
                role                  = @role,
                updated_at_utc        = @updatedAt,
                is_active             = @isActive,
                firstname             = @firstname,
                failed_login_attempts = @failedAttempts,
                locked_until_utc      = @lockedUntil,
                deleted_at            = @deletedAt
            WHERE id = @id",
            conn);

        BindParams(cmd, user);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();
        return user;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        await using var conn = await _dbContext.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT COUNT(1) FROM users WHERE email = @email", conn);
        cmd.Parameters.AddWithValue("email", email);

        return Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task<(IReadOnlyList<User> Users, int TotalCount)> GetAllAsync(
        string? emailSearch,
        string? usernameSearch,
        UserRole? role,
        bool? isActive,
        bool includeDeleted,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();
        var paramValues = new List<(string Name, object Value)>();

        if (!includeDeleted)
            conditions.Add("deleted_at IS NULL");

        if (!string.IsNullOrWhiteSpace(emailSearch))
        {
            conditions.Add("email ILIKE @email");
            paramValues.Add(("email", $"%{emailSearch}%"));
        }

        if (!string.IsNullOrWhiteSpace(usernameSearch))
        {
            conditions.Add("username ILIKE @username");
            paramValues.Add(("username", $"%{usernameSearch}%"));
        }

        if (role.HasValue)
        {
            conditions.Add("role = @role");
            paramValues.Add(("role", role.Value.ToString()));
        }

        if (isActive.HasValue)
        {
            conditions.Add("is_active = @isActive");
            paramValues.Add(("isActive", (object)isActive.Value));
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        await using var conn = await _dbContext.OpenConnectionAsync(cancellationToken);

        // Total count
        await using var countCmd = new NpgsqlCommand($"SELECT COUNT(1) FROM users {where}", conn);
        foreach (var (name, value) in paramValues)
            countCmd.Parameters.AddWithValue(name, value);
        var total = (int)Convert.ToInt64(await countCmd.ExecuteScalarAsync(cancellationToken));

        // Data page
        await using var dataCmd = new NpgsqlCommand(
            $"SELECT {SelectColumns} FROM users {where} ORDER BY created_at_utc DESC LIMIT @limit OFFSET @offset",
            conn);
        foreach (var (name, value) in paramValues)
            dataCmd.Parameters.AddWithValue(name, value);
        dataCmd.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 200));
        dataCmd.Parameters.AddWithValue("offset", Math.Max(0, offset));

        var users = new List<User>();
        await using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            users.Add(MapUser(reader));

        return (users, total);
    }

    // ── private ───────────────────────────────────────────────────────────────

    private static void BindParams(NpgsqlCommand cmd, User user)
    {
        cmd.Parameters.AddWithValue("id", user.Id.Value);
        cmd.Parameters.AddWithValue("username", user.Username);
        cmd.Parameters.AddWithValue("email", user.Email);
        cmd.Parameters.AddWithValue("passwordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("role", user.Role.ToString());
        cmd.Parameters.AddWithValue("createdAt", user.CreatedAt);
        cmd.Parameters.AddWithValue("isActive", user.IsActive);
        cmd.Parameters.AddWithValue("firstname", (object?)user.Firstname ?? DBNull.Value);
        cmd.Parameters.AddWithValue("failedAttempts", user.FailedLoginAttempts);
        cmd.Parameters.AddWithValue("lockedUntil", (object?)user.LockedUntilUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("deletedAt", (object?)user.DeletedAt ?? DBNull.Value);
    }

    private static User MapUser(NpgsqlDataReader r) => new(
        new UserId(r.GetGuid(0)),
        r.GetString(1),
        r.GetString(2),
        r.GetString(3),
        Enum.Parse<UserRole>(r.GetString(4)),
        r.GetDateTime(5),
        r.GetBoolean(6),
        r.IsDBNull(7) ? null : r.GetString(7),
        r.IsDBNull(8) ? 0 : r.GetInt32(8),
        r.IsDBNull(9) ? null : r.GetDateTime(9),
        r.IsDBNull(10) ? null : r.GetDateTime(10)
    );
}