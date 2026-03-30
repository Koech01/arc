using Arc.Domain.Models;
using Arc.Application.Results;
namespace Arc.Application.Workflows;


public interface IWorkflowExecutor
{
    ExecutionResult Execute(Workflow workflow);
}