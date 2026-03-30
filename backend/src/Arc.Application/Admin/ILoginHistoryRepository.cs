using Arc.Domain.Models;
namespace Arc.Application.Admin;


/// <summary>
/// Represents a single login attempt, successful or failed.
/// </summary>
public sealed record LoginHistoryEntry(
    long Id,
    UserId UserId,
    DateTime TimestampUtc,
    bool Success,
    string? FailureReason,
    string? IpAddress,
    string? UserAgent
);

/// <summary>
/// Repository contract for persisting and reading login history.
/// </summary>
public interface ILoginHistoryRepository
{
    /// <summary>Records a login attempt (fire-and-forget safe - implementations must never throw).</summary>
    Task RecordAsync(
        UserId userId,
        bool success,
        string? failureReason = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns login history for a user, ordered by most-recent first.</summary>
    Task<IReadOnlyList<LoginHistoryEntry>> GetByUserIdAsync(
        UserId userId,
        int limit = 50,
        CancellationToken cancellationToken = default);
}