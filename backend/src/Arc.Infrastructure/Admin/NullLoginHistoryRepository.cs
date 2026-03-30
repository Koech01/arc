using Arc.Domain.Models;
using Arc.Application.Admin;
namespace Arc.Infrastructure.Admin;


/// <summary>
/// No-op login history repository for environments without a PostgreSQL backend.
/// </summary>
public sealed class NullLoginHistoryRepository : ILoginHistoryRepository
{
    public Task RecordAsync(
        UserId userId,
        bool success,
        string? failureReason = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<LoginHistoryEntry>> GetByUserIdAsync(
        UserId userId,
        int limit = 50,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<LoginHistoryEntry>>(Array.Empty<LoginHistoryEntry>());
}