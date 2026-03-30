using System.Text;
using Arc.Domain.Models;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
namespace Arc.Application.Results;


public sealed class ExecutionResult
{
    [JsonPropertyName("executionId")]
    public string ExecutionId { get; }

    [JsonPropertyName("userId")]
    public UserId UserId { get; }

    [JsonPropertyName("tasks")]
    public IReadOnlyCollection<TaskExecutionResult> Tasks { get; }

    /// <summary>
    /// Primary constructor. Used by DeterministicExecutionEngineV1 which supplies
    /// the pre-computed unique execution ID (includes invocation timestamp ticks).
    /// </summary>
    [JsonConstructor]
    public ExecutionResult(string executionId, UserId userId, IEnumerable<TaskExecutionResult> tasks)
    {
        ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
        UserId = userId;
        Tasks = tasks?.ToArray() ?? throw new ArgumentNullException(nameof(tasks));
    }

    /// <summary>
    /// Backward-compatible constructor for infrastructure rebuilds (importer, transformer,
    /// JSON deserializer) and test helpers that do not supply an execution ID.
    /// Derives a stable ID from UserId + task IDs so the object is always valid.
    /// </summary>
    public ExecutionResult(UserId userId, IEnumerable<TaskExecutionResult> tasks)
    {
        Tasks = tasks?.ToArray() ?? throw new ArgumentNullException(nameof(tasks));
        UserId = userId;
        ExecutionId = DeriveId(userId, Tasks);
    }

    private static string DeriveId(UserId userId, IReadOnlyCollection<TaskExecutionResult> tasks)
    {
        var concatenatedIds = string.Join(",", tasks.OrderBy(t => t.ExecutionOrder).Select(t => t.TaskId));
        var input = $"{userId}|{concatenatedIds}";
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
