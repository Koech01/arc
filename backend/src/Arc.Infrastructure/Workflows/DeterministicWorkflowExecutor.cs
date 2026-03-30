using Arc.Domain.Models;
using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.Workflows;
namespace Arc.Infrastructure.Workflows;


public sealed class DeterministicWorkflowExecutor : IWorkflowExecutor
{
    private readonly IExecutionEngine _executionEngine;

    public DeterministicWorkflowExecutor(IExecutionEngine executionEngine)
    {
        _executionEngine = executionEngine ?? throw new ArgumentNullException(nameof(executionEngine));
    }

    public ExecutionResult Execute(Workflow workflow)
    {
        if (workflow is null)
            throw new ArgumentNullException(nameof(workflow));

        var executionGraph = ConvertWorkflowToExecutionGraph(workflow);
        return _executionEngine.Execute(executionGraph);
    }

    private static ExecutionGraph ConvertWorkflowToExecutionGraph(Workflow workflow)
    {
        var taskNodes = workflow.Tasks
            .Select(wt => new TaskNode(
                id: wt.Id,
                name: wt.Name,
                prompt: wt.Prompt,
                llmConfigId: wt.LLMConfigId ?? workflow.LLMConfigId,
                dependsOn: wt.Dependencies))
            .ToList();

        return new ExecutionGraph(taskNodes);
    }
}