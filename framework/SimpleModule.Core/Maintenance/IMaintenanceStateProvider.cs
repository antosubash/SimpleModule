namespace SimpleModule.Core.Maintenance;

/// <summary>
/// Reads the current maintenance-mode state. Implementations are expected to
/// be cheap (cached) so the middleware can call this once per request without
/// adding noticeable latency.
/// </summary>
public interface IMaintenanceStateProvider
{
    ValueTask<MaintenanceState?> GetAsync(CancellationToken cancellationToken = default);
}
