using Arc.Domain.Models;
using System.Collections.Concurrent;
using Arc.Application.LLM;
namespace Arc.Infrastructure.LLM;


/// <summary>
/// In-memory LLM configuration repository for lightweight tests and local helpers.
/// </summary>
public sealed class InMemoryLLMConfigurationRepository : ILLMConfigurationRepository
{
    private readonly ConcurrentDictionary<string, LLMConfiguration> _store = new();

    public Task<LLMConfiguration?> GetByIdAsync(string id, UserId userId)
    {
        _store.TryGetValue(id, out var config);
        return Task.FromResult(config);
    }

    public Task<List<LLMConfiguration>> ListByUserAsync(UserId userId)
    {
        var configs = _store.Values
            .Where(c => c.CreatedBy == userId)
            .ToList();
        return Task.FromResult(configs);
    }

    public Task CreateAsync(LLMConfiguration config)
    {
        _store[config.Id] = config;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LLMConfiguration config)
    {
        _store[config.Id] = config;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, UserId userId)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
