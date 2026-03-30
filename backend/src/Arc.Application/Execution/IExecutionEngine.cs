using Arc.Domain.Models;
using Arc.Application.Results;
namespace Arc.Application.Execution;


public interface IExecutionEngine
{
    ExecutionResult Execute(ExecutionGraph graph);
}