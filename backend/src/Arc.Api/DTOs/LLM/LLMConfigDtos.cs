namespace Arc.Api.DTOs.LLM;

public sealed record CreateLLMConfigRequestDto(
    string Name,
    string BaseUrl,
    string Model,
    string? ApiKey,
    string? Endpoint,
    string? AuthType,
    Dictionary<string, string>? Headers
);

public sealed record UpdateLLMConfigRequestDto(
    string? Name,
    string? Model,
    string? ApiKey,
    string? BaseUrl,
    string? Endpoint,
    string? AuthType,
    Dictionary<string, string>? Headers
);

public sealed record LLMConfigResponseDto(
    string Id,
    string Name,
    string BaseUrl,
    string Model,
    string Endpoint,
    string AuthType,
    bool IsActive,
    DateTime CreatedAt,
    /// <summary>
    /// Masked API key (e.g. "sk-***1234"). Null when no key is stored.
    /// The raw API key is never included in responses.
    /// </summary>
    string? MaskedApiKey = null
);

public sealed record TestLLMConnectionResponseDto(
    bool Success,
    int ResponseTimeMs,
    string Message
);