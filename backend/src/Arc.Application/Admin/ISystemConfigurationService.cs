namespace Arc.Application.Admin;


/// <summary>
/// Exposes a sanitized snapshot of the resolved application configuration.
/// No secrets or credentials are included.
/// </summary>
public interface ISystemConfigurationService
{
    /// <summary>Returns the current system configuration snapshot.</summary>
    SystemConfigSnapshot GetSnapshot();
}

/// <summary>Non-sensitive resolved configuration values made visible to admins.</summary>
public sealed record SystemConfigSnapshot(
    string DatabaseProvider,
    string LLMDefaultProvider,
    string LLMDefaultModel,
    int JwtExpirationMinutes,
    int RateLimitPermitLimit,
    int RateLimitWindowSeconds,
    bool MaintenanceModeEnabled,
    string Environment,
    string ApiVersion
);