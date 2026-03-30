using Arc.Application.LLM;
using Arc.Infrastructure.LLM;
using Arc.Application.Identity;
using Arc.Application.Execution;
using Microsoft.Extensions.Logging;
namespace Arc.Infrastructure.Execution;


/// <summary>
/// Dynamic LLM provider service that resolves providers from user-owned LLM configurations.
/// Throws InvalidOperationException if the config ID is missing, not found, or inactive -
/// no silent fallback to system defaults.
/// </summary>
public sealed class DynamicLLMProviderService : ILLMProviderService
{
    private readonly ILLMConfigurationRepository _configRepository;
    private readonly LLMProviderFactory _providerFactory;
    private readonly IUserContext _userContext;
    private readonly ILogger<DynamicLLMProviderService> _logger;

    public DynamicLLMProviderService(
        ILLMConfigurationRepository configRepository,
        LLMProviderFactory providerFactory,
        IUserContext userContext,
        ILogger<DynamicLLMProviderService> logger)
    {
        _configRepository = configRepository;
        _providerFactory = providerFactory;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<ILLMProvider> GetProviderAsync(string? llmConfigId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(llmConfigId))
        {
            throw new InvalidOperationException(
                "No LLM configuration was specified for this task. " +
                "Assign an LLM configuration to the workflow or task before executing.");
        }

        var userId = _userContext.CurrentUserId;
        var config = await _configRepository.GetByIdAsync(llmConfigId, userId);

        if (config == null)
        {
            throw new InvalidOperationException(
                $"LLM configuration '{llmConfigId}' was not found. " +
                "It may have been deleted or does not belong to the current user.");
        }

        if (!config.IsActive)
        {
            throw new InvalidOperationException(
                $"LLM configuration '{llmConfigId}' is inactive. " +
                "Activate the configuration or select a different one before executing.");
        }

        _logger.LogDebug("Using LLM config {ConfigId} ({BaseUrl}/{Model})", llmConfigId, config.BaseUrl, config.Model);
        return _providerFactory.CreateProvider(config);
    }
}