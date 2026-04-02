using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleModule.Core.Hosting;

/// <summary>
/// Calls lifecycle hooks on all discovered modules during application startup and shutdown.
/// Supports both <see cref="IModule.OnStartAsync"/>/<see cref="IModule.OnStopAsync"/> (default methods)
/// and the focused <see cref="IModuleLifecycle"/> interface.
/// </summary>
public sealed partial class ModuleLifecycleHostedService(
    IEnumerable<IModule> modules,
    IHost host,
    ILogger<ModuleLifecycleHostedService> logger
) : IHostedLifecycleService
{
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        foreach (var module in modules)
        {
            module.ConfigureHost(host);
        }
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var module in modules)
        {
            var moduleName = module.GetType().Name;
            try
            {
                if (module is IModuleLifecycle lifecycle)
                {
                    await lifecycle.OnStartAsync(cancellationToken);
                }
                else
                {
                    await module.OnStartAsync(cancellationToken);
                }
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

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Module stop failures must not prevent other modules from stopping"
    )]
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var module in modules)
        {
            var moduleName = module.GetType().Name;
            try
            {
                if (module is IModuleLifecycle lifecycle)
                {
                    await lifecycle.OnStopAsync(cancellationToken);
                }
                else
                {
                    await module.OnStopAsync(cancellationToken);
                }
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

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Module {ModuleName} failed during OnStartAsync"
    )]
    private static partial void LogModuleStartFailed(
        ILogger logger,
        string moduleName,
        Exception exception
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Module {ModuleName} stopped")]
    private static partial void LogModuleStopped(ILogger logger, string moduleName);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Module {ModuleName} failed during OnStopAsync"
    )]
    private static partial void LogModuleStopFailed(
        ILogger logger,
        string moduleName,
        Exception exception
    );
}
