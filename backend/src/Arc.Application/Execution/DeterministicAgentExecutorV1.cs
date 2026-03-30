using Arc.Domain.Models;
using Arc.Application.Results;
using Microsoft.Extensions.Logging;
namespace Arc.Application.Execution;


/// <summary>
/// Agent executor that dynamically resolves LLM providers based on task configuration.
/// Maintains determinism through LLM provider's deterministic generation.
/// </summary>
public sealed class DeterministicAgentExecutorV1 : IAgentExecutor
{
    private readonly ILLMProviderService _llmProviderService;
    private readonly ILogger<DeterministicAgentExecutorV1> _logger;

    public DeterministicAgentExecutorV1(
        ILLMProviderService llmProviderService,
        ILogger<DeterministicAgentExecutorV1> logger)
    {
        _llmProviderService = llmProviderService ?? throw new ArgumentNullException(nameof(llmProviderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TaskExecutionResult> ExecuteAsync(TaskNode taskNode, IReadOnlyDictionary<string, string> dependencyOutputs)
    {
        ArgumentNullException.ThrowIfNull(taskNode);
        ArgumentNullException.ThrowIfNull(dependencyOutputs);

        try
        {
            // Dynamically resolve LLM provider based on task configuration
            var provider = await _llmProviderService.GetProviderAsync(taskNode.LLMConfigId);

            // Use task-specific prompt if provided, otherwise fallback to generic prompt
            var prompt = !string.IsNullOrWhiteSpace(taskNode.Prompt)
                ? taskNode.Prompt
                : $"Execute the following task: {taskNode.Name}";
            
            // Substitute template variables with dependency outputs
            var resolvedPrompt = TemplateVariableSubstitution.Substitute(prompt, dependencyOutputs);
            var output = await provider.GenerateTextAsync(resolvedPrompt);

            var result = new TaskExecutionResult(
                taskNode.Id,
                taskNode.Name,
                1,
                TaskExecutionStatus.Succeeded,
                output
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task {TaskId} execution failed", taskNode.Id);

            var result = new TaskExecutionResult(
                taskNode.Id,
                taskNode.Name,
                1,
                TaskExecutionStatus.Failed,
                $"Error: {ex.Message}"
            );

            return result;
        }
    }
}