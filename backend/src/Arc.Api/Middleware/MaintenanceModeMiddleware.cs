using Arc.Application.Admin;
namespace Arc.Api.Middleware;


/// <summary>
/// Returns HTTP 503 Service Unavailable for all non-admin requests when maintenance mode
/// is active. Admin users and health-check endpoints are always allowed through.
/// </summary>
public sealed class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceModeMiddleware> _logger;

    private static readonly string[] AlwaysAllowedPrefixes =
    [
        "/api/auth/login",
        "/api/admin",
        "/health",
        "/swagger"
    ];

    public MaintenanceModeMiddleware(RequestDelegate next, ILogger<MaintenanceModeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IMaintenanceModeService maintenanceMode)
    {
        if (!maintenanceMode.IsEnabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        // Always allow admin and auth endpoints during maintenance
        if (AlwaysAllowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Allow authenticated admins through regardless of path
        if (context.User.IsInRole("Admin"))
        {
            await _next(context);
            return;
        }

        var status = maintenanceMode.GetStatus();
        _logger.LogInformation("Maintenance mode blocked request to {Path}", path);

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.Append("Retry-After", "300");
        context.Response.ContentType = "application/json";

        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            message = "Service is temporarily unavailable for maintenance. Please try again later.",
            reason = status.Reason,
            retryAfterSeconds = 300
        });

        await context.Response.WriteAsync(body);
    }
}

/// <summary>Extension method to register the maintenance mode middleware.</summary>
public static class MaintenanceModeMiddlewareExtensions
{
    public static IApplicationBuilder UseMaintenanceModeMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<MaintenanceModeMiddleware>();
}