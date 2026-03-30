using System.Text;
using Arc.Domain.Models;
using Arc.Application.Results;
using System.Security.Cryptography;
namespace Arc.Application.Execution;


public static class DeterministicTaskHasher
{
    public static string Compute(TaskNode node, IReadOnlyList<TaskExecutionResult> dependencyResults)
    {
        var sb = new StringBuilder();
        sb.Append(node.Id).Append("|").Append(node.Name);

        foreach (var dep in dependencyResults.OrderBy(d => d.TaskId))
        {
            sb.Append("|").Append(dep.TaskId).Append(":").Append(dep.Status);
        }

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}