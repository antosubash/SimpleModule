namespace SimpleModule.Hosting.Maintenance;

/// <summary>
/// Reads and writes the maintenance-mode sentinel. Implementations must be safe to call
/// from a hot middleware path; <see cref="GetState"/> in particular is invoked on every request.
/// </summary>
public interface IMaintenanceModeStore
{
    /// <summary>
    /// Returns the current sentinel snapshot, or <c>null</c> if the application is up.
    /// May serve cached data; callers should treat the value as the source of truth.
    /// </summary>
    MaintenanceModeState? GetState();

    Task EnableAsync(MaintenanceModeState state, CancellationToken cancellationToken = default);

    Task DisableAsync(CancellationToken cancellationToken = default);
}
