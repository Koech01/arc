using System.Text;
using System.Diagnostics;
using Arc.Application.Results;
using Arc.Application.Execution;
using System.Security.Cryptography;
using Arc.Application.Orchestration;
namespace Arc.Infrastructure.Execution;


/// <summary>
/// Deterministic batch executor implementation.
/// Processes multiple executions with deterministic batch ID derivation and performance metrics.
/// Each execution within the batch retains its deterministic properties.
/// </summary>
public sealed class DeterministicBatchExecutorV1 : IBatchExecutor
{
    private readonly IOrchestrator _orchestrator;
    private readonly IExecutionResultStore _resultStore;

    public DeterministicBatchExecutorV1(
        IOrchestrator orchestrator,
        IExecutionResultStore resultStore)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _resultStore = resultStore ?? throw new ArgumentNullException(nameof(resultStore));
    }

    public async Task<BatchExecutionResult> ExecuteBatchAsync(IReadOnlyCollection<string> inputs)
    {
        if (inputs is null || inputs.Count == 0)
        {
            throw new ArgumentException("Batch inputs cannot be null or empty.", nameof(inputs));
        }

        var batchStartTime = Stopwatch.GetTimestamp();
        var executions = new List<BatchExecutionItem>();
        long totalExecutionTimeMs = 0;
        int successCount = 0;
        int failureCount = 0;

        // Execute each input sequentially to preserve deterministic ordering
        int index = 0;
        foreach (var input in inputs)
        {
            try
            {
                var executionStartTime = Stopwatch.GetTimestamp();

                // Execute deterministically
                var result = _orchestrator.Execute(input);

                var executionTimeMs = GetElapsedMilliseconds(executionStartTime);
                totalExecutionTimeMs += executionTimeMs;

                // Derive execution ID deterministically
                var executionId = string.Join("-", result.Tasks.Select(t => t.TaskId));

                // Store execution result
                await _resultStore.StoreAsync(executionId, result);

                // Determine execution status from task results
                var status = result.Tasks.All(t => t.Status == TaskExecutionStatus.Succeeded)
                    ? "Succeeded"
                    : "PartiallyFailed";

                if (status == "Succeeded")
                    successCount++;
                else
                    failureCount++;

                executions.Add(new BatchExecutionItem(
                    Index: index,
                    ExecutionId: executionId,
                    Tasks: result.Tasks,
                    ExecutionTimeMs: executionTimeMs,
                    Status: status
                ));
            }
            catch (Exception ex)
            {
                failureCount++;

                // Add failed execution with error status
                executions.Add(new BatchExecutionItem(
                    Index: index,
                    ExecutionId: $"batch-failed-{index}",
                    Tasks: Array.Empty<TaskExecutionResult>(),
                    ExecutionTimeMs: GetElapsedMilliseconds(Stopwatch.GetTimestamp()),
                    Status: $"Failed: {ex.Message}"
                ));
            }

            index++;
        }

        var batchExecutionTimeMs = GetElapsedMilliseconds(batchStartTime);

        // Derive deterministic batch ID from sorted execution IDs
        var batchIdSource = string.Join("|", executions.Select(e => e.ExecutionId).OrderBy(id => id));
        var batchId = GenerateDeterministicBatchId(batchIdSource);

        var averageExecutionTimeMs = executions.Count > 0 ? totalExecutionTimeMs / executions.Count : 0;

        return new BatchExecutionResult(
            BatchId: batchId,
            CreatedAtUtc: DateTime.UtcNow,
            Executions: executions.AsReadOnly(),
            TotalExecutionTimeMs: batchExecutionTimeMs,
            AverageExecutionTimeMs: averageExecutionTimeMs,
            SuccessCount: successCount,
            FailureCount: failureCount
        );
    }

    /// <summary>
    /// Generates a deterministic batch ID using SHA256 hash of sorted execution IDs.
    /// </summary>
    private static string GenerateDeterministicBatchId(string source)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
        return "batch-" + BitConverter.ToString(hash, 0, 8).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Gets elapsed milliseconds from Stopwatch timestamp.
    /// </summary>
    private static long GetElapsedMilliseconds(long startTimestamp)
    {
        var endTimestamp = Stopwatch.GetTimestamp();
        return (long)((endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency);
    }
}
