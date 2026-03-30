using Arc.Application.Admin;
namespace Arc.Infrastructure.Admin;


/// <summary>
/// In-process, thread-safe maintenance mode service.
/// State is reset on application restart. Suitable for single-instance deployments.
/// </summary>
public sealed class InMemoryMaintenanceModeService : IMaintenanceModeService
{
    private volatile bool _isEnabled;
    private Guid? _enabledBy;
    private DateTime? _enabledAtUtc;
    private string? _reason;
    private readonly object _lock = new();

    public bool IsEnabled => _isEnabled;

    public void Enable(Guid actingAdminId, string? reason = null)
    {
        lock (_lock)
        {
            _isEnabled = true;
            _enabledBy = actingAdminId;
            _enabledAtUtc = DateTime.UtcNow;
            _reason = reason;
        }
    }

    public void Disable(Guid actingAdminId)
    {
        lock (_lock)
        {
            _isEnabled = false;
            _enabledBy = null;
            _enabledAtUtc = null;
            _reason = null;
        }
    }

    public MaintenanceModeStatus GetStatus()
    {
        lock (_lock)
        {
            return new MaintenanceModeStatus(_isEnabled, _enabledBy, _enabledAtUtc, _reason);
        }
    }
}
