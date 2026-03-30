using Arc.Application.LLM;
namespace Arc.Application.Execution;


/// <summary>
/// Service for dynamically resolving LLM providers based on configuration.
/// Supports task-level, workflow-level, and system-level fallback.
/// </summary>
public interface ILLMProviderService
{
    /// <summary>
    /// Gets an LLM provider for the given configuration ID.
    /// Falls back to default configuration if ID is null.
    /// </summary>
    Task<ILLMProvider> GetProviderAsync(string? llmConfigId, CancellationToken cancellationToken = default);
}