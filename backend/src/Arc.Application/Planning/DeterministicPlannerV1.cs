using Arc.Domain.Models;

namespace Arc.Application.Planning;

/// <summary>
/// Deterministic, rule-based planner.
/// No LLM usage. Same input always produces the same ExecutionGraph.
/// </summary>
public sealed class DeterministicPlannerV1 : IPlanner
{
    public ExecutionGraph Plan(string input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var lines = input
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        if (lines.Length == 0)
        {
            throw new ArgumentException("Input must contain at least one task.", nameof(input));
        }

        var nodes = new List<TaskNode>(lines.Length);

        for (int i = 0; i < lines.Length; i++)
        {
            var id = $"task-{i + 1}";
            var name = lines[i];

            var dependsOn = i == 0
                ? Array.Empty<string>()
                : new[] { $"task-{i}" };

            nodes.Add(new TaskNode(
                id: id,
                name: name,
                dependsOn: dependsOn
            ));
        }

        return new ExecutionGraph(nodes);
    }
}