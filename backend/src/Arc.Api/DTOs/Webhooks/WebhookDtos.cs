namespace Arc.Api.DTOs.Webhooks;


public sealed class CreateWebhookRequestDto
{
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public string Secret { get; set; } = string.Empty;
}

public sealed class WebhookResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class WebhookListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class WebhookTestResponseDto
{
    public bool Success { get; set; }
    public int ResponseCode { get; set; }
    public int ResponseTime { get; set; }
}

public sealed class ToggleWebhookRequestDto
{
    public bool IsActive { get; set; }
}

/// <summary>
/// Request DTO for partial webhook update (PATCH).
/// Secret is optional - omit or send empty string to keep the existing secret.
/// </summary>
public sealed class UpdateWebhookRequestDto
{
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    /// <summary>
    /// If absent or empty the stored secret is preserved.
    /// If provided it must be ≥ 20 characters and replaces the existing secret.
    /// </summary>
    public string? Secret { get; set; }
}