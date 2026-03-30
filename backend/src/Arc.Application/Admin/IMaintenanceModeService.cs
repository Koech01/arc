namespace Arc.Application.Admin;


/// <summary>
/// Controls system-wide maintenance mode.
/// When enabled, the MaintenanceModeMiddleware rejects non-admin requests with HTTP 503.
/// State is held in-process; it resets on restart unless a persistent backend is used.
/// </summary>
public interface IMaintenanceModeService
{
    /// <summary>True when maintenance mode is currently active.</summary>
    bool IsEnabled { get; }

    /// <summary>Enables maintenance mode.</summary>
    void Enable(Guid actingAdminId, string? reason = null);

    /// <summary>Disables maintenance mode.</summary>
    void Disable(Guid actingAdminId);

    /// <summary>Returns the current maintenance mode status snapshot.</summary>
    MaintenanceModeStatus GetStatus();
}

/// <summary>Current snapshot of maintenance mode state.</summary>
public sealed record MaintenanceModeStatus(
    bool IsEnabled,
    Guid? EnabledBy,
    DateTime? EnabledAtUtc,
    string? Reason
);