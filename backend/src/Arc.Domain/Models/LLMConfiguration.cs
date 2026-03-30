namespace Arc.Domain.Models;


/// <summary>
/// User-configured LLM endpoint. Works with any OpenAI-compatible API.
/// </summary>
public sealed class LLMConfiguration
{
    public string Id { get; }
    public string Name { get; }
    public string BaseUrl { get; }
    public string Model { get; }
    public string? ApiKey { get; }
    public string Endpoint { get; }
    public string AuthType { get; }
    public Dictionary<string, string> Headers { get; }
    public UserId CreatedBy { get; }
    public DateTime CreatedAt { get; }
    public bool IsActive { get; }

    private LLMConfiguration(
        string id,
        string name,
        string baseUrl,
        string model,
        string? apiKey,
        string endpoint,
        string authType,
        Dictionary<string, string> headers,
        UserId createdBy,
        DateTime createdAt,
        bool isActive)
    {
        Id = id;
        Name = name;
        BaseUrl = baseUrl;
        Model = model;
        ApiKey = apiKey;
        Endpoint = endpoint;
        AuthType = authType;
        Headers = headers;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        IsActive = isActive;
    }

    public static LLMConfiguration Create(
        string name,
        string baseUrl,
        string model,
        string? apiKey,
        string? endpoint,
        string? authType,
        Dictionary<string, string>? headers,
        UserId createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be empty", nameof(baseUrl));
        
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model cannot be empty", nameof(model));

        var id = GenerateId(createdBy, name);

        return new LLMConfiguration(
            id,
            name,
            baseUrl,
            model,
            apiKey,
            endpoint ?? "chat/completions",
            authType ?? "bearer",
            headers ?? new Dictionary<string, string>(),
            createdBy,
            DateTime.UtcNow,
            true
        );
    }

    public LLMConfiguration Deactivate()
    {
        return new LLMConfiguration(Id, Name, BaseUrl, Model, ApiKey, Endpoint, AuthType, Headers, CreatedBy, CreatedAt, false);
    }

    /// <summary>
    /// Returns a new instance with supplied non-null fields replaced.
    /// Pass null for any field to keep its current value.
    /// For apiKey: null or empty string → preserve existing; non-empty → replace.
    /// Id, CreatedBy, CreatedAt, and IsActive are always preserved.
    /// </summary>
    public LLMConfiguration WithUpdates(
        string? name = null,
        string? baseUrl = null,
        string? model = null,
        string? apiKey = null,
        string? endpoint = null,
        string? authType = null,
        Dictionary<string, string>? headers = null)
    {
        var resolvedApiKey = string.IsNullOrEmpty(apiKey) ? ApiKey : apiKey;

        return new LLMConfiguration(
            Id,
            name ?? Name,
            baseUrl ?? BaseUrl,
            model ?? Model,
            resolvedApiKey,
            endpoint ?? Endpoint,
            authType ?? AuthType,
            headers ?? Headers,
            CreatedBy,
            CreatedAt,
            IsActive);
    }

    private static string GenerateId(UserId userId, string name)
    {
        var input = $"{userId.Value}:{name.ToLower()}:{DateTime.UtcNow.Ticks}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..16].ToLower();
    }
}