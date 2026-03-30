using Arc.Domain.Models;
namespace Arc.Application.LLM;


/// <summary>
/// Repository for managing user LLM configurations.
/// </summary>
public interface ILLMConfigurationRepository
{
    Task<LLMConfiguration?> GetByIdAsync(string id, UserId userId);
    Task<List<LLMConfiguration>> ListByUserAsync(UserId userId);
    Task CreateAsync(LLMConfiguration config);
    Task UpdateAsync(LLMConfiguration config);
    Task DeleteAsync(string id, UserId userId);
}