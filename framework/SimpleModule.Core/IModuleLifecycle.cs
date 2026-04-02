namespace SimpleModule.Core;

/// <summary>
/// Implement this interface for startup/shutdown lifecycle hooks and health probes.
/// Preferred over overriding <see cref="IModule.OnStartAsync"/>/<see cref="IModule.OnStopAsync"/>
/// on the module class.
/// </summary>
public interface IModuleLifecycle
{
    Task OnStartAsync(CancellationToken cancellationToken);
    Task OnStopAsync(CancellationToken cancellationToken);
    Task<ModuleHealthStatus> CheckHealthAsync(CancellationToken cancellationToken) =>
        Task.FromResult(ModuleHealthStatus.Healthy);
}
