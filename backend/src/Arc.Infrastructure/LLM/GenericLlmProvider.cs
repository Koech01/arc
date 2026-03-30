using System.Text;
using System.Text.Json;
using Arc.Application.LLM;
namespace Arc.Infrastructure.LLM;
using Microsoft.Extensions.Logging;


/// <summary>
/// Generic LLM provider that works with any OpenAI-compatible API.
/// Supports multiple authentication methods and response formats.
/// </summary>
public sealed class GenericLlmProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly string _endpoint;
    private readonly string _authType;
    private readonly string? _apiKey;
    private readonly Dictionary<string, string> _headers;
    private readonly ILogger<GenericLlmProvider> _logger;

    public GenericLlmProvider(
        HttpClient httpClient,
        string baseUrl,
        string model,
        string? apiKey,
        string endpoint,
        string authType,
        Dictionary<string, string> headers,
        ILogger<GenericLlmProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _endpoint = endpoint ?? "chat/completions";
        _authType = authType?.ToLower() ?? "bearer";
        _apiKey = apiKey;
        _headers = headers ?? new Dictionary<string, string>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (prompt is null)
            throw new ArgumentNullException(nameof(prompt));

        try
        {
            using var request = BuildRequest(prompt);
            _logger.LogInformation("Calling LLM API: {Method} {Url}", request.Method, request.RequestUri);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("LLM API error {Status}: {Error}", response.StatusCode, error);
                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = ExtractText(json);
            
            _logger.LogDebug("LLM response received: {Length} chars", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM API call failed for model {Model}", _model);
            throw new InvalidOperationException($"LLM API call failed: {ex.Message}", ex);
        }
    }

    private HttpRequestMessage BuildRequest(string prompt)
    {
        var baseUrl = _baseUrl.TrimEnd('/');
        var endpoint = _endpoint.TrimStart('/');
        var fullUrl = $"{baseUrl}/{endpoint}";

        // Add API key to URL if needed
        if (_authType == "url-param" && !string.IsNullOrWhiteSpace(_apiKey))
        {
            var separator = fullUrl.Contains('?') ? "&" : "?";
            fullUrl = $"{fullUrl}{separator}key={_apiKey}";
        }

        var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);

        // Apply custom headers
        foreach (var header in _headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Apply authentication
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            switch (_authType)
            {
                case "bearer":
                    request.Headers.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
                    break;
                case "api-key":
                    request.Headers.Add("api-key", _apiKey);
                    break;
                case "x-api-key":
                    request.Headers.Add("x-api-key", _apiKey);
                    break;
                case "x-goog-api-key":
                    request.Headers.Add("x-goog-api-key", _apiKey);
                    break;
            }
        }

        // Build payload based on endpoint format
        object payload;
        if (endpoint.Contains("generateContent") || baseUrl.Contains("generativelanguage.googleapis.com"))
        {
            // Gemini format
            payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };
        }
        else
        {
            // OpenAI-compatible format (default)
            payload = new
            {
                model = _model,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0,
                max_tokens = 128,
                stream = false
            };
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static string ExtractText(string json)
    {
        using var doc = JsonDocument.Parse(json);

        // Try OpenAI-compatible format
        if (doc.RootElement.TryGetProperty("choices", out var choices))
        {
            return choices[0].GetProperty("message").GetProperty("content").GetString()?.Trim() ?? string.Empty;
        }

        // Try Gemini format
        if (doc.RootElement.TryGetProperty("candidates", out var candidates))
        {
            return candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()?.Trim() ?? string.Empty;
        }

        // Try Ollama format
        if (doc.RootElement.TryGetProperty("response", out var response))
        {
            return response.GetString()?.Trim() ?? string.Empty;
        }

        // Try HuggingFace format
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            return doc.RootElement[0].GetProperty("generated_text").GetString()?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }
}