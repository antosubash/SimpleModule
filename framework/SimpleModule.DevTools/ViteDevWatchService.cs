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
    IHostEnvironment environment
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
                () => RunBuild(moduleName, "npx vite build", moduleDir)
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
                () => RunBuild("ClientApp", "npx vite build", clientAppDir)
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

    private async Task RunBuild(string name, string command, string workingDir)
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
        }
        else
        {
            LogBuildFailed(logger, name);
        }
    }

    private async Task RunTailwindBuild()
    {
        var tailwindCli = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(_repoRoot, "tools", "tailwindcss.exe")
            : Path.Combine(_repoRoot, "tools", "tailwindcss");

        var hostDir = environment.ContentRootPath;
        var inputPath = Path.Combine(hostDir, "Styles", "app.css");
        var outputPath = Path.Combine(hostDir, "wwwroot", "css", "app.css");

        var command = $"\"{tailwindCli}\" -i \"{inputPath}\" -o \"{outputPath}\"";

        await RunBuild("Tailwind", command, hostDir).ConfigureAwait(false);
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

    private async Task<bool> RunProcessAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken stoppingToken
    )
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.StartInfo.Environment["VITE_MODE"] = "dev";

            var existingPath = process.StartInfo.Environment.TryGetValue("PATH", out var path)
                ? path
                : "";
            process.StartInfo.Environment["PATH"] =
                $"{_npmBinPath}{Path.PathSeparator}{existingPath}";

            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(stoppingToken);
            var stderrTask = process.StandardError.ReadToEndAsync(stoppingToken);

            await process.WaitForExitAsync(stoppingToken).ConfigureAwait(false);

            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var trimmedStderr = stderr.Trim();
                if (trimmedStderr.Length > 0)
                {
                    LogBuildStderr(logger, trimmedStderr);
                }

                var trimmedStdout = stdout.Trim();
                if (trimmedStdout.Length > 0)
                {
                    LogBuildStdout(logger, trimmedStdout);
                }

                return false;
            }

            var trimmedOutput = stdout.Trim();
            if (trimmedOutput.Length > 0)
            {
                LogBuildOutput(logger, trimmedOutput);
            }

            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogBuildCancelled(logger);
            return false;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogBuildProcessFailed(logger, ex, fileName, arguments);
            return false;
        }
    }

    private static string? FindRepoRoot(string startPath)
    {
        var current = startPath;
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current, ".git")))
            {
                return current;
            }

            current = Path.GetDirectoryName(current);
        }

        return null;
    }

    private static List<string> DiscoverModuleDirectories(string modulesRoot)
    {
        var directories = new List<string>();

        if (!Directory.Exists(modulesRoot))
        {
            return directories;
        }

        foreach (var moduleGroupDir in Directory.GetDirectories(modulesRoot))
        {
            var srcDir = Path.Combine(moduleGroupDir, "src");
            if (!Directory.Exists(srcDir))
            {
                continue;
            }

            foreach (var moduleDir in Directory.GetDirectories(srcDir))
            {
                if (File.Exists(Path.Combine(moduleDir, "vite.config.ts")))
                {
                    directories.Add(moduleDir);
                }
            }
        }

        return directories;
    }

    private static bool ShouldIgnoreModulePath(string fullPath)
    {
        return ContainsSegment(fullPath, "wwwroot") || ContainsSegment(fullPath, "node_modules");
    }

    private static bool ShouldIgnoreClientAppPath(string fullPath)
    {
        return ContainsSegment(fullPath, "node_modules");
    }

    private static bool ShouldIgnoreTailwindPath(string fullPath)
    {
        return ContainsSegment(fullPath, "_scan");
    }

    private static bool ContainsSegment(string fullPath, string segment)
    {
        // Check both separator styles for cross-platform path matching
        return fullPath.Contains(
                $"{Path.DirectorySeparatorChar}{segment}{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase
            )
            || fullPath.Contains(
                $"{Path.AltDirectorySeparatorChar}{segment}{Path.AltDirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static string GetShellFileName()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "sh";
    }

    private static string GetShellArguments(string command)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"/c {command}"
            : $"-c \"{command}\"";
    }

    #region LoggerMessage definitions

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not find repository root (.git directory). ViteDevWatchService will not start."
    )]
    private static partial void LogRepoRootNotFound(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "ViteDevWatchService starting. Repo root: {RepoRoot}"
    )]
    private static partial void LogServiceStarting(ILogger logger, string repoRoot);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "ViteDevWatchService watching {ModuleCount} module(s), ClientApp, and Styles"
    )]
    private static partial void LogWatchingStarted(ILogger logger, int moduleCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{BuildKey} file changed: {FilePath}")]
    private static partial void LogFileChanged(
        ILogger logger,
        string buildKey,
        string? filePath
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Watching module: {ModuleName} at {ModuleDir}"
    )]
    private static partial void LogWatchingModule(
        ILogger logger,
        string moduleName,
        string moduleDir
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Watching ClientApp at {ClientAppDir}")]
    private static partial void LogWatchingClientApp(ILogger logger, string clientAppDir);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Watching Styles at {StylesDir}")]
    private static partial void LogWatchingStyles(ILogger logger, string stylesDir);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rebuilding {Name}...")]
    private static partial void LogRebuilding(ILogger logger, string name);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{Name} rebuilt successfully in {ElapsedMs}ms"
    )]
    private static partial void LogRebuiltSuccessfully(
        ILogger logger,
        string name,
        long elapsedMs
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "{Name} build failed")]
    private static partial void LogBuildFailed(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Build stderr: {Stderr}")]
    private static partial void LogBuildStderr(ILogger logger, string stderr);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Build stdout: {Stdout}")]
    private static partial void LogBuildStdout(ILogger logger, string stdout);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Build output: {Stdout}")]
    private static partial void LogBuildOutput(ILogger logger, string stdout);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Build cancelled")]
    private static partial void LogBuildCancelled(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to run build process: {FileName} {Arguments}"
    )]
    private static partial void LogBuildProcessFailed(
        ILogger logger,
        Exception ex,
        string fileName,
        string arguments
    );

    #endregion
}
