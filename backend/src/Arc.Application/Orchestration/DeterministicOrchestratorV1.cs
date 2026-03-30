using Arc.Application.Results;
using Arc.Application.Planning;
using Arc.Application.Execution;
namespace Arc.Application.Orchestration;


/// <summary>
/// Pure deterministic orchestrator.
/// Coordinates planning and execution with no side effects.
/// </summary>
public sealed class DeterministicOrchestratorV1 : IOrchestrator
{
    private readonly IPlanner _planner;
    private readonly IExecutionEngine _executionEngine;

    public DeterministicOrchestratorV1(
        IPlanner planner,
        IExecutionEngine executionEngine)
    {
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _executionEngine = executionEngine ?? throw new ArgumentNullException(nameof(executionEngine));
    }

    public ExecutionResult Execute(string input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var graph = _planner.Plan(input);
        return _executionEngine.Execute(graph);
    }
}