using Arc.Domain.Models;
using Arc.Application.LLM;
namespace Arc.Infrastructure.LLM;
using Microsoft.Extensions.Logging;


/// <summary>
/// Factory for creating generic LLM provider instances.
/// </summary>
public class LLMProviderFactory
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;

    public LLMProviderFactory(HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    public virtual ILLMProvider CreateProvider(LLMConfiguration config)
    {
        var logger = _loggerFactory.CreateLogger<GenericLlmProvider>();
        
        return new GenericLlmProvider(
            _httpClient,
            config.BaseUrl,
            config.Model,
            config.ApiKey,
            config.Endpoint,
            config.AuthType,
            config.Headers,
            logger
        );
    }
}