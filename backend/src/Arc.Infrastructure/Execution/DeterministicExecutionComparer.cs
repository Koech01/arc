using Arc.Application.Results;
using Arc.Application.Execution;
namespace Arc.Infrastructure.Execution;


/// <summary>
/// Deterministic execution comparer implementation.
/// Provides detailed task-by-task comparison and diff analysis.
/// </summary>
public sealed class DeterministicExecutionComparer : IExecutionComparer
{
    private readonly IExecutionResultStore _resultStore;

    public DeterministicExecutionComparer(IExecutionResultStore resultStore)
    {
        _resultStore = resultStore ?? throw new ArgumentNullException(nameof(resultStore));
    }

    public async Task<ExecutionComparisonResult?> CompareAsync(string executionId1, string executionId2)
    {
        if (string.IsNullOrWhiteSpace(executionId1))
            throw new ArgumentException("ExecutionId1 cannot be null or empty.", nameof(executionId1));

        if (string.IsNullOrWhiteSpace(executionId2))
            throw new ArgumentException("ExecutionId2 cannot be null or empty.", nameof(executionId2));

        if (executionId1 == executionId2)
            throw new ArgumentException("ExecutionId1 and ExecutionId2 must be different.", nameof(executionId1));

        // Retrieve both executions
        var result1 = await _resultStore.GetAsync(executionId1);
        var result2 = await _resultStore.GetAsync(executionId2);

        if (result1 is null || result2 is null)
            return null;

        // Perform task-by-task comparison
        var comparisons = CompareTasks(result1, result2);

        // Calculate metrics
        var metrics = CalculateMetrics(comparisons, result1, result2);

        // Generate summary
        var summary = GenerateSummary(metrics, executionId1, executionId2);

        return new ExecutionComparisonResult(
            ExecutionId1: executionId1,
            ExecutionId2: executionId2,
            TaskComparisons: comparisons,
            Metrics: metrics,
            Summary: summary
        );
    }

    private List<TaskComparisonItem> CompareTasks(ExecutionResult result1, ExecutionResult result2)
    {
        var comparisons = new List<TaskComparisonItem>();

        // Create lookup dictionaries for O(1) access
        var tasks1 = result1.Tasks.ToDictionary(t => t.TaskId);
        var tasks2 = result2.Tasks.ToDictionary(t => t.TaskId);

        // Get all unique task IDs in execution order
        var allTaskIds = new List<string>();
        allTaskIds.AddRange(result1.Tasks.Select(t => t.TaskId));
        foreach (var taskId in result2.Tasks.Select(t => t.TaskId))
        {
            if (!allTaskIds.Contains(taskId))
                allTaskIds.Add(taskId);
        }

        // Compare each task
        for (int i = 0; i < allTaskIds.Count; i++)
        {
            var taskId = allTaskIds[i];
            tasks1.TryGetValue(taskId, out var task1);
            tasks2.TryGetValue(taskId, out var task2);

            var status1 = task1?.Status.ToString() ?? "Missing";
            var status2 = task2?.Status.ToString() ?? "Missing";
            var output1 = task1?.Output ?? "";
            var output2 = task2?.Output ?? "";
            var order1 = task1?.ExecutionOrder ?? -1;
            var order2 = task2?.ExecutionOrder ?? -1;

            var isDifferent = status1 != status2 || output1 != output2 || order1 != order2;

            comparisons.Add(new TaskComparisonItem(
                TaskId: taskId,
                ExecutionIndex: i,
                Status1: status1,
                Status2: status2,
                Output1: output1,
                Output2: output2,
                ExecutionOrder1: order1,
                ExecutionOrder2: order2,
                IsDifferent: isDifferent
            ));
        }

        return comparisons;
    }

    private ExecutionDiffMetrics CalculateMetrics(
        List<TaskComparisonItem> comparisons,
        ExecutionResult result1,
        ExecutionResult result2)
    {
        var taskCount = comparisons.Count;
        var identicalTasks = comparisons.Count(c => !c.IsDifferent);
        var differentTasks = taskCount - identicalTasks;

        // Find divergence point (first difference)
        var divergencePointIndex = comparisons.FindIndex(c => c.IsDifferent);
        if (divergencePointIndex == -1)
            divergencePointIndex = -1; // No divergence

        var sameTaskCount = result1.Tasks.Count == result2.Tasks.Count;
        var sameExecutionOrder = result1.Tasks
            .SequenceEqual(result2.Tasks, new TaskOrderComparer());

        var similarityPercentage = taskCount > 0
            ? (identicalTasks / (double)taskCount) * 100.0
            : 100.0;

        return new ExecutionDiffMetrics(
            TaskCount: taskCount,
            IdenticalTasks: identicalTasks,
            DifferentTasks: differentTasks,
            DivergencePointIndex: divergencePointIndex,
            SameTaskCount: sameTaskCount,
            SameExecutionOrder: sameExecutionOrder,
            SimilarityPercentage: similarityPercentage
        );
    }

    private string GenerateSummary(ExecutionDiffMetrics metrics, string executionId1, string executionId2)
    {
        if (metrics.DifferentTasks == 0)
        {
            return $"Executions are identical: {executionId1} == {executionId2}";
        }

        var divergenceInfo = metrics.DivergencePointIndex >= 0
            ? $", divergence at task index {metrics.DivergencePointIndex}"
            : "";

        return $"{metrics.IdenticalTasks}/{metrics.TaskCount} tasks identical ({metrics.SimilarityPercentage:F1}% similar){divergenceInfo}";
    }

    /// <summary>
    /// Compares task execution order between two executions.
    /// </summary>
    private sealed class TaskOrderComparer : IEqualityComparer<TaskExecutionResult>
    {
        public bool Equals(TaskExecutionResult? x, TaskExecutionResult? y)
        {
            if (x is null || y is null)
                return x == y;

            return x.TaskId == y.TaskId && x.ExecutionOrder == y.ExecutionOrder;
        }

        public int GetHashCode(TaskExecutionResult obj)
        {
            return HashCode.Combine(obj.TaskId, obj.ExecutionOrder);
        }
    }
}
