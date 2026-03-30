using System.Data;
using Arc.Domain.Models;
using Microsoft.Data.Sqlite;
using Arc.Application.Identity;
namespace Arc.Infrastructure.Identity;


/// <summary>
/// SQLite-based user repository.
/// Stores failed_login_attempts, locked_until_utc, and deleted_at alongside the
/// existing user fields to support account lockout and soft-delete.
/// </summary>
public sealed class SqliteUserRepository : IUserRepository
{
    private readonly string _connectionString;

    public SqliteUserRepository(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path cannot be null or empty", nameof(databasePath));

        _connectionString = $"Data Source={databasePath}";
        InitializeDatabase();
    }

    public async Task<User> CreateAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO Users
                (Id, Username, Email, PasswordHash, Role, CreatedAt, IsActive, FailedLoginAttempts, LockedUntilUtc, DeletedAt)
            VALUES
                (@Id, @Username, @Email, @PasswordHash, @Role, @CreatedAt, @IsActive,
                 @FailedLoginAttempts, @LockedUntilUtc, @DeletedAt)";

        using var cmd = new SqliteCommand(sql, connection);
        BindUserParams(cmd, user);
        await cmd.ExecuteNonQueryAsync();
        return user;
    }

    public async Task<User?> GetByIdAsync(UserId userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = new SqliteCommand(SelectColumns("WHERE Id = @Id"), connection);
        cmd.Parameters.AddWithValue("@Id", userId.Value.ToString());

        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapUserFromReader(reader) : null;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = new SqliteCommand(SelectColumns("WHERE Email = @Email"), connection);
        cmd.Parameters.AddWithValue("@Email", email.ToLowerInvariant());

        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapUserFromReader(reader) : null;
    }

    public async Task<User> UpdateAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            UPDATE Users
            SET Username           = @Username,
                Email              = @Email,
                PasswordHash       = @PasswordHash,
                Role               = @Role,
                IsActive           = @IsActive,
                FailedLoginAttempts = @FailedLoginAttempts,
                LockedUntilUtc     = @LockedUntilUtc,
                DeletedAt          = @DeletedAt
            WHERE Id = @Id";

        using var cmd = new SqliteCommand(sql, connection);
        BindUserParams(cmd, user);

        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows == 0)
            throw new InvalidOperationException($"User with ID {user.Id} not found");

        return user;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = new SqliteCommand("SELECT COUNT(1) FROM Users WHERE Email = @Email", connection);
        cmd.Parameters.AddWithValue("@Email", email.ToLowerInvariant());

        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
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
        if (!includeDeleted) conditions.Add("DeletedAt IS NULL");
        if (!string.IsNullOrWhiteSpace(emailSearch)) conditions.Add("Email LIKE @email");
        if (!string.IsNullOrWhiteSpace(usernameSearch)) conditions.Add("Username LIKE @username");
        if (role.HasValue) conditions.Add("Role = @role");
        if (isActive.HasValue) conditions.Add("IsActive = @isActive");

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Total count
        using var countCmd = new SqliteCommand($"SELECT COUNT(1) FROM Users {where}", connection);
        ApplyFilterParams(countCmd, emailSearch, usernameSearch, role, isActive);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));

        // Data page
        var dataSql = $@"
            SELECT Id, Username, Email, PasswordHash, Role, CreatedAt, IsActive,
                   FailedLoginAttempts, LockedUntilUtc, DeletedAt
            FROM Users {where}
            ORDER BY CreatedAt DESC
            LIMIT @limit OFFSET @offset";

        using var dataCmd = new SqliteCommand(dataSql, connection);
        ApplyFilterParams(dataCmd, emailSearch, usernameSearch, role, isActive);
        dataCmd.Parameters.AddWithValue("@limit", limit);
        dataCmd.Parameters.AddWithValue("@offset", offset);

        var users = new List<User>();
        using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            users.Add(MapUserFromReader(reader));

        return (users, total);
    }

    // private helpers

    private static string SelectColumns(string predicate) => $@"
        SELECT Id, Username, Email, PasswordHash, Role, CreatedAt, IsActive,
               FailedLoginAttempts, LockedUntilUtc, DeletedAt
        FROM Users {predicate}";

    private static void BindUserParams(SqliteCommand cmd, User user)
    {
        cmd.Parameters.AddWithValue("@Id", user.Id.Value.ToString());
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@Role", (int)user.Role);
        cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
        cmd.Parameters.AddWithValue("@FailedLoginAttempts", user.FailedLoginAttempts);
        cmd.Parameters.AddWithValue("@LockedUntilUtc",
            (object?)user.LockedUntilUtc?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DeletedAt",
            (object?)user.DeletedAt?.ToString("O") ?? DBNull.Value);
    }

    private static void ApplyFilterParams(
        SqliteCommand cmd, string? emailSearch, string? usernameSearch, UserRole? role, bool? isActive)
    {
        if (!string.IsNullOrWhiteSpace(emailSearch))
            cmd.Parameters.AddWithValue("@email", $"%{emailSearch}%");
        if (!string.IsNullOrWhiteSpace(usernameSearch))
            cmd.Parameters.AddWithValue("@username", $"%{usernameSearch}%");
        if (role.HasValue)
            cmd.Parameters.AddWithValue("@role", (int)role.Value);
        if (isActive.HasValue)
            cmd.Parameters.AddWithValue("@isActive", isActive.Value);
    }

    private void InitializeDatabase()
    {
        var dir = Path.GetDirectoryName(_connectionString.Replace("Data Source=", ""));
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string create = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id                  TEXT PRIMARY KEY,
                Username            TEXT NOT NULL,
                Email               TEXT NOT NULL UNIQUE,
                PasswordHash        TEXT NOT NULL,
                Role                INTEGER NOT NULL,
                CreatedAt           TEXT NOT NULL,
                IsActive            INTEGER NOT NULL,
                FailedLoginAttempts INTEGER NOT NULL DEFAULT 0,
                LockedUntilUtc      TEXT,
                DeletedAt           TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_Users_Email    ON Users(Email);
            CREATE INDEX IF NOT EXISTS IX_Users_Username ON Users(Username);
            CREATE INDEX IF NOT EXISTS IX_Users_IsActive ON Users(IsActive);";

        using var createCmd = new SqliteCommand(create, connection);
        createCmd.ExecuteNonQuery();

        // Idempotent migrations for pre-existing databases
        foreach (var ddl in new[]
        {
            "ALTER TABLE Users ADD COLUMN FailedLoginAttempts INTEGER NOT NULL DEFAULT 0",
            "ALTER TABLE Users ADD COLUMN LockedUntilUtc TEXT",
            "ALTER TABLE Users ADD COLUMN DeletedAt TEXT"
        })
        {
            try
            {
                using var alter = new SqliteCommand(ddl, connection);
                alter.ExecuteNonQuery();
            }
            catch { /* column already exists */ }
        }
    }

    private static User MapUserFromReader(IDataReader reader)
    {
        var id = UserId.From(Guid.Parse(reader.GetString(0)));
        var username = reader.GetString(1);
        var email = reader.GetString(2);
        var passwordHash = reader.GetString(3);
        var role = (UserRole)reader.GetInt32(4);
        var createdAt = DateTime.Parse(reader.GetString(5));
        var isActive = reader.GetBoolean(6);
        var failedAttempts = reader.IsDBNull(7) ? 0 : reader.GetInt32(7);
        DateTime? lockedUntil = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8));
        DateTime? deletedAt = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9));

        return new User(id, username, email, passwordHash, role, createdAt, isActive,
            firstname: null, failedAttempts, lockedUntil, deletedAt);
    }
}