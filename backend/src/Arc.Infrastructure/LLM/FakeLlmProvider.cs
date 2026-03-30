using System.Text;
using Arc.Application.LLM;
using System.Security.Cryptography;


namespace Arc.Infrastructure.LLM
{
    public sealed class FakeLlmProvider : ILLMProvider
    {
        public Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (prompt is null)
                throw new ArgumentNullException(nameof(prompt));

            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(prompt));
            var hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            var result = $"Generated response for prompt [{prompt.Substring(0, Math.Min(30, prompt.Length))}...] -> {hashHex[..16]}";
            return Task.FromResult(result);
        }
    }
}