using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.Telemetry;
namespace Arc.Infrastructure.Execution;


/// <summary>
/// Deterministic execution profiler that analyzes audit logs and execution results
/// to generate comprehensive performance profiles with task metrics, critical path analysis,
/// and resource utilization patterns.
/// </summary>
public sealed class DeterministicExecutionProfiler : IExecutionProfiler
{
    private readonly IExecutionResultStore _resultStore;
    private readonly IAuditLogger _auditLogger;

    public DeterministicExecutionProfiler(
        IExecutionResultStore resultStore,
        IAuditLogger auditLogger)
    {
        _resultStore = resultStore ?? throw new ArgumentNullException(nameof(resultStore));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    }

    public async Task<ExecutionPerformanceProfile?> GenerateProfileAsync(string executionId)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            throw new ArgumentException("ExecutionId cannot be null or whitespace.", nameof(executionId));

        // Retrieve execution result and audit logs
        var executionResult = await _resultStore.GetAsync(executionId);
        if (executionResult == null)
            return null;

        var auditLogs = await _auditLogger.GetExecutionLogsAsync(executionId);
        if (auditLogs.Count == 0)
            return null;

        // Compute task performance metrics
        var taskMetrics = ComputeTaskPerformanceMetrics(executionResult, auditLogs);

        // Analyze critical path (also updates IsCriticalPath flags)
        var (criticalPath, updatedTaskMetrics) = AnalyzeCriticalPath(taskMetrics);

        // Compute resource utilization
        var resourceUtilization = ComputeResourceUtilization(updatedTaskMetrics, auditLogs);

        return new ExecutionPerformanceProfile(
            executionId,
            updatedTaskMetrics,
            criticalPath,
            resourceUtilization,
            DateTime.UtcNow
        );
    }

    private static IReadOnlyList<TaskPerformanceMetrics> ComputeTaskPerformanceMetrics(
        ExecutionResult executionResult,
        IReadOnlyList<AuditLogEntry> auditLogs)
    {
        var taskMetrics = new List<TaskPerformanceMetrics>();
        var taskStartTimes = new Dictionary<string, DateTime>();
        var taskEndTimes = new Dictionary<string, DateTime>();

        // Extract task start/end times from audit logs
        foreach (var log in auditLogs)
        {
            if (log.TaskId == null) continue;

            switch (log.EventType)
            {
                case AuditEventType.TaskStarted:
                    taskStartTimes[log.TaskId] = log.TimestampUtc;
                    break;
                case AuditEventType.TaskFinished:
                    taskEndTimes[log.TaskId] = log.TimestampUtc;
                    break;
            }
        }

        // Build dependency map from execution results
        var dependencyMap = BuildDependencyMap(executionResult);

        // Compute metrics for each task
        foreach (var task in executionResult.Tasks.OrderBy(t => t.ExecutionOrder))
        {
            var executionTimeMs = ComputeTaskExecutionTime(task.TaskId, taskStartTimes, taskEndTimes);
            var dependencyWaitTimeMs = ComputeDependencyWaitTime(task, taskEndTimes, taskStartTimes, dependencyMap);
            var dependencies = dependencyMap.GetValueOrDefault(task.TaskId, new List<string>());

            taskMetrics.Add(new TaskPerformanceMetrics(
                task.TaskId,
                task.TaskName,
                task.ExecutionOrder,
                executionTimeMs,
                dependencyWaitTimeMs,
                false, // Will be updated in critical path analysis
                dependencies
            ));
        }

        return taskMetrics;
    }

    private static Dictionary<string, List<string>> BuildDependencyMap(ExecutionResult executionResult)
    {
        // Since we don't have direct access to the original ExecutionGraph dependencies,
        // we infer dependencies from execution order and assume sequential execution
        var dependencyMap = new Dictionary<string, List<string>>();
        var orderedTasks = executionResult.Tasks.OrderBy(t => t.ExecutionOrder).ToList();

        for (int i = 0; i < orderedTasks.Count; i++)
        {
            var currentTask = orderedTasks[i];
            var dependencies = new List<string>();

            // For deterministic behavior, assume each task depends on all previous tasks
            // This is a conservative approach that ensures deterministic profiling
            for (int j = 0; j < i; j++)
            {
                dependencies.Add(orderedTasks[j].TaskId);
            }

            dependencyMap[currentTask.TaskId] = dependencies;
        }

        return dependencyMap;
    }

    private static long ComputeTaskExecutionTime(
        string taskId,
        Dictionary<string, DateTime> startTimes,
        Dictionary<string, DateTime> endTimes)
    {
        if (!startTimes.TryGetValue(taskId, out var startTime) ||
            !endTimes.TryGetValue(taskId, out var endTime))
        {
            return 0; // Default to 0 if timing data is missing
        }

        return (long)(endTime - startTime).TotalMilliseconds;
    }

    private static long ComputeDependencyWaitTime(
        TaskExecutionResult task,
        Dictionary<string, DateTime> taskEndTimes,
        Dictionary<string, DateTime> taskStartTimes,
        Dictionary<string, List<string>> dependencyMap)
    {
        if (!taskStartTimes.TryGetValue(task.TaskId, out var taskStartTime))
            return 0;

        var dependencies = dependencyMap.GetValueOrDefault(task.TaskId, new List<string>());
        if (dependencies.Count == 0)
            return 0;

        // Find the latest dependency completion time
        var latestDependencyEndTime = DateTime.MinValue;
        foreach (var depId in dependencies)
        {
            if (taskEndTimes.TryGetValue(depId, out var depEndTime) && depEndTime > latestDependencyEndTime)
            {
                latestDependencyEndTime = depEndTime;
            }
        }

        if (latestDependencyEndTime == DateTime.MinValue)
            return 0;

        // Wait time is the difference between when dependencies finished and when this task started
        var waitTime = (taskStartTime - latestDependencyEndTime).TotalMilliseconds;
        return Math.Max(0, (long)waitTime);
    }

    private static (CriticalPathAnalysis, IReadOnlyList<TaskPerformanceMetrics>) AnalyzeCriticalPath(IReadOnlyList<TaskPerformanceMetrics> taskMetrics)
    {
        var criticalPathTasks = taskMetrics
            .OrderBy(t => t.ExecutionOrder)
            .Select(t => t.TaskId)
            .ToList();

        var totalCriticalPathTime = taskMetrics.Sum(t => t.ExecutionTimeMs);

        var updatedTaskMetrics = taskMetrics.Select(t => t with { IsCriticalPath = true }).ToList();

        var analysis = new CriticalPathAnalysis(
            criticalPathTasks,
            totalCriticalPathTime,
            100.0
        );

        return (analysis, updatedTaskMetrics);
    }

    private static ResourceUtilizationMetrics ComputeResourceUtilization(
        IReadOnlyList<TaskPerformanceMetrics> taskMetrics,
        IReadOnlyList<AuditLogEntry> auditLogs)
    {
        var totalExecutionTime = taskMetrics.Sum(t => t.ExecutionTimeMs);
        var averageTaskExecutionTime = taskMetrics.Count > 0 
            ? (double)totalExecutionTime / taskMetrics.Count 
            : 0.0;

        // In sequential execution, parallelizable time is 0 and sequential time is total
        var parallelizableTime = 0L;
        var sequentialTime = totalExecutionTime;

        // Parallelization efficiency is 0% for sequential execution
        var parallelizationEfficiency = 0.0;

        // Max concurrent tasks is 1 for sequential execution
        var maxConcurrentTasks = 1;

        return new ResourceUtilizationMetrics(
            totalExecutionTime,
            parallelizableTime,
            sequentialTime,
            parallelizationEfficiency,
            maxConcurrentTasks,
            averageTaskExecutionTime
        );
    }
}