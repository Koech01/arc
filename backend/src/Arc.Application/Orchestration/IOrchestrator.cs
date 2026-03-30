using Arc.Application.Results;
namespace Arc.Application.Orchestration;


public interface IOrchestrator
{
    ExecutionResult Execute(string input);
}