using Arc.Domain.Models;
using Arc.Application.Results;
namespace Arc.Application.Execution;


public interface IAgentExecutor
{
    Task<TaskExecutionResult> ExecuteAsync(TaskNode taskNode, IReadOnlyDictionary<string, string> dependencyOutputs);
}