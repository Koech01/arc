using Arc.Application.Admin;
namespace Arc.Infrastructure.Admin;
using Microsoft.Extensions.Configuration;


/// <summary>
/// Builds a sanitized configuration snapshot from the resolved <see cref="IConfiguration"/>.
/// No secrets, connection strings, or API keys are ever included.
/// </summary>
public sealed class SystemConfigurationService : ISystemConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IMaintenanceModeService _maintenanceModeService;

    public SystemConfigurationService(IConfiguration configuration, IMaintenanceModeService maintenanceModeService)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _maintenanceModeService = maintenanceModeService ?? throw new ArgumentNullException(nameof(maintenanceModeService));
    }

    public SystemConfigSnapshot GetSnapshot()
    {
        var llmDefault = _configuration["LLM:DefaultProvider"] ??
                         (_configuration["HuggingFace:ApiKey"] is not null ? "HuggingFace" : "Fake");

        var llmModel = _configuration["LLM:DefaultModel"] ??
                       _configuration["HuggingFace:Model"] ?? "N/A";

        var jwtExpiry = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var exp) ? exp : 60;

        var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown";

        return new SystemConfigSnapshot(
            DatabaseProvider: "PostgreSQL",
            LLMDefaultProvider: llmDefault,
            LLMDefaultModel: llmModel,
            JwtExpirationMinutes: jwtExpiry,
            RateLimitPermitLimit: 50,
            RateLimitWindowSeconds: 60,
            MaintenanceModeEnabled: _maintenanceModeService.IsEnabled,
            Environment: environment,
            ApiVersion: "1.0"
        );
    }
}