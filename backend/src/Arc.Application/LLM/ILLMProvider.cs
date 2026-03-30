namespace Arc.Application.LLM;


/// <summary>
/// Port for deterministic text generation providers.
/// Implementations must produce the same output for the same input.
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Generates deterministic text based on a given prompt asynchronously.
    /// </summary>
    /// <param name="prompt">Input prompt text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deterministic output string.</returns>
    Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default);
}