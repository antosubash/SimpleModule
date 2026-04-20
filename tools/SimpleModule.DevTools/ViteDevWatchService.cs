using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleModule.DevTools;

/// <summary>
/// Background service that watches module and ClientApp source files for changes,
/// then triggers Vite rebuilds and Tailwind CSS compilation automatically.
/// Only intended for use in Development environments.
/// </summary>
public sealed partial class ViteDevWatchService(
    ILogger<ViteDevWatchService> logger,
    IHostEnvironment environment,
    LiveReloadServer liveReloadServer
) : BackgroundService
{
    private static readonly string[] FrontendExtensions =
    [
        "*.ts",
        "*.tsx",
        "*.css",
        "*.jsx",
        "*.js",
    ];

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceTimers = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly ConcurrentDictionary<string, bool> _buildInProgress = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly List<FileSystemWatcher> _watchers = [];
    private CancellationToken _stoppingToken;
    private string _repoRoot = null!;
    private string _npmBinPath = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var repoRoot = FindRepoRoot(environment.ContentRootPath);
        if (repoRoot is null)
        {
            LogRepoRootNotFound(logger);
            return;
        }

        _stoppingToken = stoppingToken;
        _repoRoot = repoRoot;
        _npmBinPath = Path.Combine(repoRoot, "node_modules", ".bin");
        LogServiceStarting(logger, repoRoot);

        var modulesRoot = Path.Combine(repoRoot, "modules");
        var moduleDirectories = DiscoverModuleDirectories(modulesRoot);

        foreach (var moduleDir in moduleDirectories)
        {
            var moduleName = Path.GetFileName(moduleDir);
            SetupWatcher(
                $"module:{moduleName}",
                moduleDir,
                FrontendExtensions,
                ShouldIgnoreModulePath,
                () => RunBuild(moduleName, "npx vite build --configLoader runner", moduleDir)
            );
            LogWatchingModule(logger, moduleName, moduleDir);
        }

        var clientAppDir = Path.Combine(environment.ContentRootPath, "ClientApp");
        if (Directory.Exists(clientAppDir))
        {
            SetupWatcher(
                "clientapp",
                clientAppDir,
                FrontendExtensions,
                ShouldIgnoreClientAppPath,
                () => RunBuild("ClientApp", "npx vite build --configLoader runner", clientAppDir)
            );
            LogWatchingClientApp(logger, clientAppDir);
        }

        var stylesDir = Path.Combine(environment.ContentRootPath, "Styles");
        if (Directory.Exists(stylesDir))
        {
            SetupWatcher(
                "tailwind",
                stylesDir,
                ["*.css"],
                ShouldIgnoreTailwindPath,
                () => RunTailwindBuild()
            );
            LogWatchingStyles(logger, stylesDir);
        }

        LogWatchingStarted(logger, moduleDirectories.Count);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected shutdown
        }
    }

    public override void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _watchers.Clear();

        foreach (var cts in _debounceTimers.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _debounceTimers.Clear();

        base.Dispose();
    }

    private void SetupWatcher(
        string buildKey,
        string watchDir,
        string[] filters,
        Func<string, bool> shouldIgnore,
        Func<Task> buildAction
    )
    {
        var watcher = new FileSystemWatcher(watchDir)
        {
            IncludeSubdirectories = true,
            NotifyFilter =
                NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
        };

        foreach (var filter in filters)
        {
            watcher.Filters.Add(filter);
        }

        void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (shouldIgnore(e.FullPath))
            {
                return;
            }

            LogFileChanged(logger, buildKey, e.Name);
            DebouncedBuild(buildKey, buildAction);
        }

        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Renamed += (sender, e) => OnChanged(sender, e);

        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);
    }

    private async Task RunBuild(
        string name,
        string command,
        string workingDir,
        ReloadType reloadType = ReloadType.Full
    )
    {
        LogRebuilding(logger, name);
        var stopwatch = Stopwatch.StartNew();

        var success = await RunProcessAsync(
                GetShellFileName(),
                GetShellArguments(command),
                workingDir,
                _stoppingToken
            )
            .ConfigureAwait(false);

        stopwatch.Stop();

        if (success)
        {
            LogRebuiltSuccessfully(logger, name, stopwatch.ElapsedMilliseconds);
            await liveReloadServer.NotifyReloadAsync(reloadType, name).ConfigureAwait(false);
        }
        else
        {
            LogBuildFailed(logger, name);
        }
    }

    private async Task RunTailwindBuild()
    {
        var hostDir = environment.ContentRootPath;
        var inputPath = Path.Combine(hostDir, "Styles", "app.css");
        var outputPath = Path.Combine(hostDir, "wwwroot", "css", "app.css");

        var tailwindBin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "tailwindcss.cmd"
            : "tailwindcss";
        var tailwindCli = Path.Combine(_npmBinPath, tailwindBin);
        var command = $"\"{tailwindCli}\" -i \"{inputPath}\" -o \"{outputPath}\"";

        await RunBuild("Tailwind", command, hostDir, ReloadType.CssOnly).ConfigureAwait(false);
    }

    private void DebouncedBuild(string buildKey, Func<Task> buildAction)
    {
        if (_debounceTimers.TryRemove(buildKey, out var previousCts))
        {
            previousCts.Cancel();
            previousCts.Dispose();
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken);
        _debounceTimers[buildKey] = cts;

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(300), cts.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                _debounceTimers.TryRemove(buildKey, out _);

                if (!_buildInProgress.TryAdd(buildKey, true))
                {
                    return;
                }

                try
                {
                    await buildAction().ConfigureAwait(false);
                }
                finally
                {
                    _buildInProgress.TryRemove(buildKey, out _);
                }
            },
            _stoppingToken
        );
    }
}
