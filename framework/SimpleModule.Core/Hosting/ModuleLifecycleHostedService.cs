using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleModule.Core.Hosting;

/// <summary>
/// Calls <see cref="IModule.OnStartAsync"/> and <see cref="IModule.OnStopAsync"/> lifecycle hooks
/// on all discovered modules during application startup and shutdown.
/// </summary>
public sealed partial class ModuleLifecycleHostedService(
    IEnumerable<IModule> modules,
    ILogger<ModuleLifecycleHostedService> logger
) : IHostedLifecycleService
{
    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var module in modules)
        {
            var moduleName = module.GetType().Name;
            try
            {
                await module.OnStartAsync(cancellationToken);
                LogModuleStarted(logger, moduleName);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogModuleStartFailed(logger, moduleName, ex);
                throw;
            }
        }
    }

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Module stop failures must not prevent other modules from stopping")]
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var module in modules)
        {
            var moduleName = module.GetType().Name;
            try
            {
                await module.OnStopAsync(cancellationToken);
                LogModuleStopped(logger, moduleName);
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested — continue stopping remaining modules
            }
            catch (Exception ex)
            {
                LogModuleStopFailed(logger, moduleName, ex);
            }
        }
    }

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Module {ModuleName} started")]
    private static partial void LogModuleStarted(ILogger logger, string moduleName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Module {ModuleName} failed during OnStartAsync")]
    private static partial void LogModuleStartFailed(ILogger logger, string moduleName, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Module {ModuleName} stopped")]
    private static partial void LogModuleStopped(ILogger logger, string moduleName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Module {ModuleName} failed during OnStopAsync")]
    private static partial void LogModuleStopFailed(ILogger logger, string moduleName, Exception exception);
}
