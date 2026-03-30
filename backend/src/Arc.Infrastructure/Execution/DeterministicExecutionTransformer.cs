using System.Text;
using System.Text.Json;
using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;
using System.Security.Cryptography;
namespace Arc.Infrastructure.Execution;


/// <summary>
/// Deterministic execution transformer that applies transformation rules
/// and generates stable transformed execution IDs.
/// </summary>
public sealed class DeterministicExecutionTransformer : IExecutionTransformer
{
    private readonly IExecutionResultStore _executionResultStore;

    public DeterministicExecutionTransformer(IExecutionResultStore executionResultStore)
    {
        _executionResultStore = executionResultStore ?? throw new ArgumentNullException(nameof(executionResultStore));
    }

    public async Task<ExecutionTransformationResult> TransformAsync(
        string executionId, 
        ExecutionTransformationRules transformationRules)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            throw new ArgumentException("ExecutionId cannot be null or empty.", nameof(executionId));

        if (transformationRules is null)
            throw new ArgumentNullException(nameof(transformationRules));

        // Retrieve original execution
        var originalExecution = await _executionResultStore.GetAsync(executionId);
        if (originalExecution is null)
            throw new InvalidOperationException($"Execution with ID '{executionId}' not found.");

        // Build original execution graph from tasks
        var originalGraph = BuildExecutionGraphFromTasks(originalExecution.Tasks);

        // Apply transformations
        var transformedGraph = ApplyTransformations(originalGraph, transformationRules);

        // Generate deterministic transformed execution ID
        var transformedExecutionId = GenerateTransformedExecutionId(executionId, transformationRules);

        // Create transformed execution result
        var transformedExecution = CreateTransformedExecution(originalExecution, transformedGraph, transformationRules);

        // Store transformed execution
        await _executionResultStore.StoreAsync(transformedExecutionId, transformedExecution);

        return new ExecutionTransformationResult(
            transformedExecutionId,
            transformedGraph,
            transformedExecution
        );
    }

    private ExecutionGraph BuildExecutionGraphFromTasks(IReadOnlyCollection<TaskExecutionResult> tasks)
    {
        // Build a flat graph with no inferred dependencies.
        // Dependency rewiring rules will establish the actual edges.
        // Execution order is preserved via topological sort on the transformed graph.
        var taskNodes = tasks
            .OrderBy(t => t.ExecutionOrder)
            .Select(t => new TaskNode(t.TaskId, t.TaskName, null, null, new List<string>()))
            .ToList();

        return new ExecutionGraph(taskNodes);
    }

    private ExecutionGraph ApplyTransformations(ExecutionGraph originalGraph, ExecutionTransformationRules rules)
    {
        var transformedNodes = new List<TaskNode>();

        foreach (var originalNode in originalGraph.Nodes)
        {
            // Apply task mapping
            var mappingRule = rules.TaskMappings.FirstOrDefault(m => m.SourceTaskId == originalNode.Id);
            var nodeId = mappingRule?.TargetTaskId ?? originalNode.Id;
            var nodeName = mappingRule?.TargetTaskName ?? originalNode.Name;

            // Apply dependency rewiring
            var rewiringRule = rules.DependencyRewiring.FirstOrDefault(r => r.TaskId == originalNode.Id);
            var dependencies = rewiringRule?.NewDependencies ?? originalNode.DependsOn;

            // Apply task mapping to dependencies as well
            var mappedDependencies = dependencies.Select(dep =>
            {
                var depMappingRule = rules.TaskMappings.FirstOrDefault(m => m.SourceTaskId == dep);
                return depMappingRule?.TargetTaskId ?? dep;
            }).ToList();

            transformedNodes.Add(new TaskNode(nodeId, nodeName, null, null, mappedDependencies));
        }

        return new ExecutionGraph(transformedNodes);
    }

    private ExecutionResult CreateTransformedExecution(
        ExecutionResult originalExecution, 
        ExecutionGraph transformedGraph,
        ExecutionTransformationRules rules)
    {
        var transformedTasks = new List<TaskExecutionResult>();
        var executionOrder = 1;

        // Create tasks in topological order
        var sortedNodes = TopologicalSort(transformedGraph);

        foreach (var node in sortedNodes)
        {
            // Find corresponding original task (reverse mapping)
            var originalTaskId = GetOriginalTaskId(node.Id, rules.TaskMappings);
            var originalTask = originalExecution.Tasks.FirstOrDefault(t => t.TaskId == originalTaskId);

            var transformedTask = new TaskExecutionResult(
                TaskId: node.Id,
                TaskName: node.Name,
                ExecutionOrder: executionOrder++,
                Status: originalTask?.Status ?? TaskExecutionStatus.Succeeded,
                Output: originalTask?.Output ?? $"Transformed from {originalTaskId}"
            );

            transformedTasks.Add(transformedTask);
        }

        return new ExecutionResult(originalExecution.UserId, transformedTasks);
    }

    private string GetOriginalTaskId(string transformedTaskId, IReadOnlyCollection<TaskMappingRule> mappings)
    {
        var mappingRule = mappings.FirstOrDefault(m => m.TargetTaskId == transformedTaskId);
        return mappingRule?.SourceTaskId ?? transformedTaskId;
    }

    private IReadOnlyList<TaskNode> TopologicalSort(ExecutionGraph graph)
    {
        var result = new List<TaskNode>();
        var visited = new HashSet<string>();
        var nodeDict = graph.Nodes.ToDictionary(n => n.Id);

        void Visit(TaskNode node)
        {
            if (visited.Contains(node.Id))
                return;

            visited.Add(node.Id);

            foreach (var depId in node.DependsOn)
            {
                if (nodeDict.TryGetValue(depId, out var depNode))
                {
                    Visit(depNode);
                }
            }

            result.Add(node);
        }

        foreach (var node in graph.Nodes.OrderBy(n => n.Id))
        {
            Visit(node);
        }

        return result;
    }

    private string GenerateTransformedExecutionId(string originalExecutionId, ExecutionTransformationRules rules)
    {
        // Create deterministic hash from original ID and transformation rules
        var transformationData = new
        {
            OriginalExecutionId = originalExecutionId,
            TaskMappings = rules.TaskMappings.OrderBy(m => m.SourceTaskId).ToArray(),
            DependencyRewiring = rules.DependencyRewiring.OrderBy(r => r.TaskId).ToArray()
        };

        var json = JsonSerializer.Serialize(transformationData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return $"transformed-{hashString[..16]}";
    }
}