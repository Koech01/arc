using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.Telemetry;
namespace Arc.Infrastructure.Execution;


/// <summary>
/// Deterministic execution visualizer that generates structured visualization data
/// from execution results and audit logs for workflow analysis and debugging.
/// </summary>
public sealed class DeterministicExecutionVisualizer : IExecutionVisualizer
{
    private readonly IExecutionResultStore _resultStore;
    private readonly IAuditLogger _auditLogger;
    private readonly IExecutionProfiler _profiler;

    public DeterministicExecutionVisualizer(
        IExecutionResultStore resultStore,
        IAuditLogger auditLogger,
        IExecutionProfiler profiler)
    {
        _resultStore = resultStore ?? throw new ArgumentNullException(nameof(resultStore));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
    }

    public async Task<ExecutionVisualization?> GenerateVisualizationAsync(string executionId)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            throw new ArgumentException("ExecutionId cannot be null or whitespace.", nameof(executionId));

        var executionResult = await _resultStore.GetAsync(executionId);
        if (executionResult == null)
            return null;

        var auditLogs = await _auditLogger.GetExecutionLogsAsync(executionId);
        if (auditLogs.Count == 0)
            return null;

        var profile = await _profiler.GenerateProfileAsync(executionId);
        if (profile == null)
            return null;

        var dependencyGraph = GenerateDependencyGraph(executionResult, profile);
        var timeline = GenerateExecutionTimeline(auditLogs, profile);
        var criticalPath = profile.CriticalPath.CriticalPathTaskIds;
        var resourceAllocation = GenerateResourceAllocation(auditLogs);

        return new ExecutionVisualization(
            executionId,
            dependencyGraph,
            timeline,
            criticalPath,
            resourceAllocation,
            DateTime.UtcNow
        );
    }

    private static IReadOnlyList<TaskGraphNode> GenerateDependencyGraph(
        ExecutionResult executionResult,
        ExecutionPerformanceProfile profile)
    {
        var nodes = new List<TaskGraphNode>();
        var taskMetricsMap = profile.TaskMetrics.ToDictionary(t => t.TaskId);

        foreach (var task in executionResult.Tasks.OrderBy(t => t.ExecutionOrder))
        {
            var metrics = taskMetricsMap.GetValueOrDefault(task.TaskId);
            var dependencies = metrics?.Dependencies ?? new List<string>();
            var isCriticalPath = metrics?.IsCriticalPath ?? false;
            var executionTime = metrics?.ExecutionTimeMs ?? 0;

            nodes.Add(new TaskGraphNode(
                task.TaskId,
                task.TaskName,
                task.ExecutionOrder,
                task.Status.ToString(),
                dependencies,
                isCriticalPath,
                executionTime
            ));
        }

        return nodes;
    }

    private static IReadOnlyList<TimelineEvent> GenerateExecutionTimeline(
        IReadOnlyList<AuditLogEntry> auditLogs,
        ExecutionPerformanceProfile profile)
    {
        var events = new List<TimelineEvent>();
        var taskStartTimes = new Dictionary<string, DateTime>();
        var taskMetricsMap = profile.TaskMetrics.ToDictionary(t => t.TaskId);

        foreach (var log in auditLogs.OrderBy(l => l.TimestampUtc))
        {
            if (log.TaskId == null) continue;

            switch (log.EventType)
            {
                case AuditEventType.TaskStarted:
                    taskStartTimes[log.TaskId] = log.TimestampUtc;
                    break;

                case AuditEventType.TaskFinished:
                    if (taskStartTimes.TryGetValue(log.TaskId, out var startTime))
                    {
                        var metrics = taskMetricsMap.GetValueOrDefault(log.TaskId);
                        var taskName = metrics?.TaskName ?? log.TaskId;
                        var isCriticalPath = metrics?.IsCriticalPath ?? false;
                        var duration = Math.Max(1L, (long)(log.TimestampUtc - startTime).TotalMilliseconds);

                        events.Add(new TimelineEvent(
                            log.TaskId,
                            taskName,
                            startTime,
                            log.TimestampUtc,
                            duration,
                            "TaskExecution",
                            isCriticalPath
                        ));
                    }
                    break;
            }
        }

        return events.OrderBy(e => e.StartTime).ToList();
    }

    private static IReadOnlyList<ResourceSnapshot> GenerateResourceAllocation(
        IReadOnlyList<AuditLogEntry> auditLogs)
    {
        var snapshots = new List<ResourceSnapshot>();
        var activeTasks = new HashSet<string>();
        var lastTimestamp = DateTime.MinValue;

        foreach (var log in auditLogs.OrderBy(l => l.TimestampUtc))
        {
            if (log.TaskId == null) continue;

            var changed = false;
            switch (log.EventType)
            {
                case AuditEventType.TaskStarted:
                    activeTasks.Add(log.TaskId);
                    changed = true;
                    break;

                case AuditEventType.TaskFinished:
                    activeTasks.Remove(log.TaskId);
                    changed = true;
                    break;
            }

            if (changed && log.TimestampUtc != lastTimestamp)
            {
                snapshots.Add(new ResourceSnapshot(
                    log.TimestampUtc,
                    activeTasks.Count,
                    activeTasks.ToList()
                ));
                lastTimestamp = log.TimestampUtc;
            }
        }

        return snapshots;
    }
}