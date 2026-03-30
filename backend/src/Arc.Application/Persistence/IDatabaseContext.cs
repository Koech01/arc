using Npgsql;
namespace Arc.Application.Persistence;


/// <summary>
/// Database context abstraction for Clean Architecture.
/// Defines contract for database operations without infrastructure leakage.
/// </summary>
public interface IDatabaseContext
{
    /// <summary>
    /// Opens a new database connection.
    /// </summary>
    Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a database connection (alias for OpenConnectionAsync for consistency).
    /// </summary>
    Task<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        return OpenConnectionAsync(cancellationToken);
    }
    
    /// <summary>
    /// Executes a database migration or initialization script.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ensures database is initialized before operations.
    /// </summary>
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Checks if the database connection is healthy.
    /// </summary>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}