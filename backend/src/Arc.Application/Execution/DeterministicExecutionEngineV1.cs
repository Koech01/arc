using System.Text;
using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Identity;
using Arc.Application.Webhooks;
using Arc.Application.Telemetry;
using System.Security.Cryptography;
using Arc.Application.Notifications;


namespace Arc.Application.Execution
{
    public sealed class DeterministicExecutionEngineV1 : IExecutionEngine
    {
        private readonly IAgentExecutor _agentExecutor;
        private readonly IAuditLogger _auditLogger;
        private readonly ITaskExecutionCache _cache;
        private readonly IUserContext _userContext;
        private readonly IWebhookDispatcher _webhookDispatcher;
        private readonly INotificationService _notificationService;

        public DeterministicExecutionEngineV1(
            IAgentExecutor agentExecutor,
            IAuditLogger auditLogger,
            ITaskExecutionCache cache,
            IUserContext userContext,
            IWebhookDispatcher webhookDispatcher,
            INotificationService notificationService)
        {
            _agentExecutor = agentExecutor ?? throw new ArgumentNullException(nameof(agentExecutor));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _webhookDispatcher = webhookDispatcher ?? throw new ArgumentNullException(nameof(webhookDispatcher));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public ExecutionResult Execute(ExecutionGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (graph.Nodes.Count == 0)
                throw new InvalidOperationException("ExecutionGraph must contain at least one node.");

            var userId = _userContext.CurrentUserId;

            // Compute deterministic, dependency-respecting order
            var orderedNodes = ComputeExecutionOrder(graph);

            // Generate unique ExecutionId: UserId + graph structure + invocation timestamp ticks.
            // Ticks ensure two executions of the same workflow by the same user produce distinct IDs
            // and are both persisted rather than the second overwriting the first.
            var startTicks = DateTime.UtcNow.Ticks;
            string executionId = GenerateDeterministicExecutionId(userId, orderedNodes, startTicks);

            var startTime = new DateTime(startTicks, DateTimeKind.Utc);
            var taskResults = new List<TaskExecutionResult>();
            int executionIndex = 1;

            try
            {
                // Log orchestrator start, dispatch webhook event, and create notification
                _auditLogger.LogAsync(executionId, AuditEventType.OrchestratorStarted, message: "Execution started")
                            .GetAwaiter().GetResult();

                DispatchWebhookEvent(executionId, WebhookEventType.ExecutionStarted, userId, orderedNodes.Count, "running", 0, startTime)
                    .GetAwaiter().GetResult();

                _notificationService.NotifyExecutionStartedAsync(userId, executionId, orderedNodes.Count)
                    .GetAwaiter().GetResult();

                foreach (var node in orderedNodes)
                {
                    _auditLogger.LogAsync(executionId, AuditEventType.TaskStarted, node.Id, "Task execution started")
                        .GetAwaiter().GetResult();

                    var dependencyResults = taskResults
                        .Where(r => node.DependsOn.Contains(r.TaskId))
                        .ToList();

                    var taskHash = DeterministicTaskHasher.Compute(node, (IReadOnlyList<TaskExecutionResult>)dependencyResults);

                    var cached = _cache.GetAsync(taskHash).GetAwaiter().GetResult();

                    TaskExecutionResult result;

                    if (cached is not null)
                    {
                        result = cached with { ExecutionOrder = executionIndex };

                        _auditLogger.LogAsync(executionId, AuditEventType.TaskFinished, node.Id, "Task result loaded from cache")
                            .GetAwaiter().GetResult();
                    }
                    else
                    {
                        // Build dependency outputs map for template variable substitution
                        var dependencyOutputs = dependencyResults.ToDictionary(
                            r => r.TaskId,
                            r => r.Output ?? string.Empty
                        );

                        result = _agentExecutor.ExecuteAsync(node, dependencyOutputs).GetAwaiter().GetResult();
                        result = result with { ExecutionOrder = executionIndex };

                        _cache.StoreAsync(taskHash, result, DateTime.UtcNow.AddMinutes(10))
                            .GetAwaiter().GetResult();

                        _auditLogger.LogAsync(executionId, AuditEventType.TaskFinished, node.Id, "Task executed and cached")
                            .GetAwaiter().GetResult();
                    }

                    taskResults.Add(result);

                    // Notify the user that this individual task has finished.
                    // Indicates whether the result was served from cache or freshly executed.
                    _notificationService.NotifyTaskCompletedAsync(
                            userId, executionId, node.Id, node.Name, cached is not null)
                        .GetAwaiter().GetResult();

                    executionIndex++;
                }

                // Log orchestrator finish, dispatch webhook event, and create notification for success
                _auditLogger.LogAsync(executionId, AuditEventType.OrchestratorFinished, message: "Execution finished")
                            .GetAwaiter().GetResult();

                var duration = DateTime.UtcNow - startTime;
                DispatchWebhookEvent(executionId, WebhookEventType.ExecutionCompleted, userId, taskResults.Count, "success", duration.Milliseconds, startTime)
                    .GetAwaiter().GetResult();

                _notificationService.NotifyExecutionCompletedAsync(userId, executionId, taskResults.Count, duration.Milliseconds)
                    .GetAwaiter().GetResult();

                return new ExecutionResult(executionId, userId, taskResults);
            }
            catch (Exception ex)
            {
                // Log failure, dispatch webhook event, and create notification for failure
                var duration = DateTime.UtcNow - startTime;
                DispatchWebhookEvent(executionId, WebhookEventType.ExecutionFailed, userId, taskResults.Count, "failed", duration.Milliseconds, startTime, ex.Message)
                    .GetAwaiter().GetResult();

                _notificationService.NotifyExecutionFailedAsync(userId, executionId, ex.Message)
                    .GetAwaiter().GetResult();

                throw;
            }
        }

        /// <summary>
        /// Dispatches a webhook event asynchronously for execution lifecycle events.
        /// Does not throw exceptions; logs them instead to preserve execution result.
        /// </summary>
        private async Task DispatchWebhookEvent(
            string executionId,
            WebhookEventType eventType,
            UserId userId,
            int taskCount,
            string status,
            long durationMs,
            DateTime startTime,
            string? errorMessage = null)
        {
            try
            {
                var payload = new WebhookEventPayload(
                    executionId,
                    eventType,
                    startTime,
                    userId,
                    taskCount,
                    status,
                    durationMs,
                    errorMessage);

                await _webhookDispatcher.DispatchAsync(payload, CancellationToken.None);
            }
            catch (Exception)
            {
                // Log but don't throw - don't let webhook failures affect execution
                // Webhook dispatch is fire-and-forget
            }
        }

        /// <summary>
        /// Generates a unique execution ID by hashing UserId + topologically ordered node IDs + invocation timestamp ticks.
        /// The timestamp ensures two executions of the same workflow by the same user produce distinct IDs
        /// and are both persisted rather than the second overwriting the first.
        /// </summary>
        private static string GenerateDeterministicExecutionId(UserId userId, IReadOnlyList<TaskNode> orderedNodes, long ticks)
        {
            var concatenatedIds = string.Join(",", orderedNodes.Select(n => n.Id));
            var input = $"{userId}|{concatenatedIds}|{ticks}";
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Deterministically orders TaskNodes respecting dependencies using Kahn's algorithm.
        /// Nodes with zero dependencies are sorted by Id for determinism.
        /// </summary>
        private static IReadOnlyList<TaskNode> ComputeExecutionOrder(ExecutionGraph graph)
        {
            var nodesById = graph.Nodes.ToDictionary(n => n.Id, n => n);
            var inDegree = new Dictionary<string, int>();
            var adjacency = new Dictionary<string, List<string>>();

            foreach (var node in graph.Nodes)
            {
                inDegree[node.Id] = 0;
                adjacency[node.Id] = new List<string>();
            }

            foreach (var node in graph.Nodes)
            {
                foreach (var depId in node.DependsOn)
                {
                    adjacency[depId].Add(node.Id);
                    inDegree[node.Id]++;
                }
            }

            var zeroInDegree = new SortedSet<string>(
                inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key)
            );

            var orderedNodes = new List<TaskNode>();

            while (zeroInDegree.Count > 0)
            {
                var currentId = zeroInDegree.Min!;
                zeroInDegree.Remove(currentId);
                orderedNodes.Add(nodesById[currentId]);

                foreach (var neighbor in adjacency[currentId])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                        zeroInDegree.Add(neighbor);
                }
            }

            if (orderedNodes.Count != graph.Nodes.Count)
                throw new InvalidOperationException("ExecutionGraph contains a cycle or missing dependency.");

            return orderedNodes;
        }
    }
}