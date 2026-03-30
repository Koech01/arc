using Arc.Domain.Exceptions;
namespace Arc.Domain.Models;
using System.Text.Json.Serialization;


public sealed class ExecutionGraph
{
    private readonly Dictionary<string, TaskNode> _nodes;

    public IReadOnlyCollection<TaskNode> Nodes => _nodes.Values;

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    public ExecutionGraph()
    {
        _nodes = new Dictionary<string, TaskNode>();
    }

    public ExecutionGraph(IEnumerable<TaskNode> nodes)
    {
        if (nodes is null)
            throw new ExecutionGraphInvalidException("ExecutionGraph requires nodes.");

        _nodes = nodes.ToDictionary(n => n.Id);

        if (_nodes.Count == 0)
            throw new ExecutionGraphInvalidException("ExecutionGraph must contain at least one node.");

        ValidateDependencies();
        ValidateAcyclic();
    }

    /// <summary>
    /// Property setter for JSON deserialization.
    /// Validates graph after deserialization.
    /// </summary>
    [JsonInclude]
    public IReadOnlyCollection<TaskNode> NodesForSerialization
    {
        get => Nodes;
        init
        {
            if (value is null)
                throw new ExecutionGraphInvalidException("ExecutionGraph requires nodes.");

            _nodes.Clear();
            foreach (var node in value)
            {
                _nodes[node.Id] = node;
            }

            if (_nodes.Count == 0)
                throw new ExecutionGraphInvalidException("ExecutionGraph must contain at least one node.");

            ValidateDependencies();
            ValidateAcyclic();
        }
    }

    private void ValidateDependencies()
    // Ensures all dependencies exist
    {
        foreach (var node in _nodes.Values)
        {
            foreach (var dependencyId in node.DependsOn)
            {
                if (!_nodes.ContainsKey(dependencyId))
                {
                    throw new ExecutionGraphInvalidException(
                        $"TaskNode '{node.Id}' depends on unknown node '{dependencyId}'.");
                }
            }
        }
    }

    private void ValidateAcyclic()
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var nodeId in _nodes.Keys)
        {
            if (DetectCycle(nodeId, visited, recursionStack))
            {
                throw new ExecutionGraphInvalidException("ExecutionGraph contains a cycle.");
            }
        }
    }

    private bool DetectCycle(
        // Ensures no cycles
        string nodeId,
        HashSet<string> visited,
        HashSet<string> stack)
    {
        if (stack.Contains(nodeId))
            return true;

        if (visited.Contains(nodeId))
            return false;

        visited.Add(nodeId);
        stack.Add(nodeId);

        foreach (var dependency in _nodes[nodeId].DependsOn)
        {
            if (DetectCycle(dependency, visited, stack))
                return true;
        }

        stack.Remove(nodeId);
        return false;
    }
}

public sealed class ExecutionGraphInvalidException : DomainException
{
    public ExecutionGraphInvalidException(string message) : base(message)
    {
    }
}