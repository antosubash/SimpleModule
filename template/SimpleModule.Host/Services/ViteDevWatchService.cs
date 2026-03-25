using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SimpleModule.Host.Services;

/// <summary>
/// Background service that watches module and ClientApp source files for changes,
/// then triggers Vite rebuilds and Tailwind CSS compilation automatically.
/// Only intended for use in Development environments.
/// </summary>
public sealed partial class ViteDevWatchService(
    ILogger<ViteDevWatchService> logger,
    IWebHostEnvironment environment
) : BackgroundService
{
    private static readonly string[] WatchedExtensions = ["*.ts", "*.tsx", "*.css", "*.jsx", "*.js"];

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceTimers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, bool> _buildInProgress = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FileSystemWatcher> _watchers = [];
    private CancellationToken _stoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var repoRoot = FindRepoRoot(environment.ContentRootPath);
        if (repoRoot is null)
        {
            LogRepoRootNotFound(logger);
            return;
        }

        _stoppingToken = stoppingToken;
        LogServiceStarting(logger, repoRoot);

        // Discover all module directories with vite.config.ts
        var modulesRoot = Path.Combine(repoRoot, "modules");
        var moduleDirectories = DiscoverModuleDirectories(modulesRoot);

        foreach (var moduleDir in moduleDirectories)
        {
            var moduleName = Path.GetFileName(moduleDir);
            SetupModuleWatcher(moduleDir, moduleName, repoRoot);
        }

        // Watch ClientApp
        var clientAppDir = Path.Combine(environment.ContentRootPath, "ClientApp");
        if (Directory.Exists(clientAppDir))
        {
            SetupClientAppWatcher(clientAppDir, repoRoot);
        }

        // Watch Styles/ for Tailwind CSS changes
        var stylesDir = Path.Combine(environment.ContentRootPath, "Styles");
        if (Directory.Exists(stylesDir))
        {
            SetupTailwindWatcher(stylesDir, repoRoot);
        }

        LogWatchingStarted(logger, moduleDirectories.Count);

        // Keep the service alive until cancellation
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

        // Look for modules/*/src/*/vite.config.ts
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

    private void SetupModuleWatcher(string moduleDir, string moduleName, string repoRoot)
    {
        var buildKey = $"module:{moduleName}";
        var watcher = CreateWatcher(moduleDir, WatchedExtensions);

        void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnoreModulePath(e.FullPath))
            {
                return;
            }

            LogModuleFileChanged(logger, moduleName, e.Name);
            DebouncedBuild(buildKey, () => RunViteBuild(moduleName, moduleDir, repoRoot, _stoppingToken));
        }

        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Renamed += (sender, e) => OnChanged(sender, e);

        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);

        LogWatchingModule(logger, moduleName, moduleDir);
    }

    private void SetupClientAppWatcher(string clientAppDir, string repoRoot)
    {
        const string buildKey = "clientapp";
        var watcher = CreateWatcher(clientAppDir, WatchedExtensions);

        void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnoreClientAppPath(e.FullPath))
            {
                return;
            }

            LogClientAppFileChanged(logger, e.Name);
            DebouncedBuild(buildKey, () => RunClientAppBuild(clientAppDir, repoRoot, _stoppingToken));
        }

        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Renamed += (sender, e) => OnChanged(sender, e);

        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);

        LogWatchingClientApp(logger, clientAppDir);
    }

    private void SetupTailwindWatcher(string stylesDir, string repoRoot)
    {
        const string buildKey = "tailwind";
        var watcher = CreateWatcher(stylesDir, ["*.css"]);

        void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnoreTailwindPath(e.FullPath))
            {
                return;
            }

            LogStylesFileChanged(logger, e.Name);
            DebouncedBuild(buildKey, () => RunTailwindBuild(repoRoot, _stoppingToken));
        }

        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Renamed += (sender, e) => OnChanged(sender, e);

        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);

        LogWatchingStyles(logger, stylesDir);
    }

    private static FileSystemWatcher CreateWatcher(string path, string[] filters)
    {
        var watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
        };

        foreach (var filter in filters)
        {
            watcher.Filters.Add(filter);
        }

        return watcher;
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
        var separator1 = $"{Path.DirectorySeparatorChar}{segment}{Path.DirectorySeparatorChar}";
        var separator2 = $"{Path.AltDirectorySeparatorChar}{segment}{Path.AltDirectorySeparatorChar}";

        return fullPath.Contains(separator1, StringComparison.OrdinalIgnoreCase)
               || fullPath.Contains(separator2, StringComparison.OrdinalIgnoreCase);
    }

    private void DebouncedBuild(string buildKey, Func<Task> buildAction)
    {
        // Cancel any previous debounce timer for this build key
        if (_debounceTimers.TryRemove(buildKey, out var previousCts))
        {
            previousCts.Cancel();
            previousCts.Dispose();
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken);
        _debounceTimers[buildKey] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                // Wait for debounce period — cancelled if a newer change arrives
                await Task.Delay(TimeSpan.FromMilliseconds(300), cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Either a newer change superseded this one, or the service is shutting down
                return;
            }

            _debounceTimers.TryRemove(buildKey, out _);

            // Skip if a build is already running for this key
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
        }, _stoppingToken);
    }

    private async Task RunViteBuild(
        string moduleName,
        string moduleDir,
        string repoRoot,
        CancellationToken stoppingToken)
    {
        LogRebuildingModule(logger, moduleName);
        var stopwatch = Stopwatch.StartNew();

        var success = await RunProcessAsync(
            GetShellFileName(),
            GetShellArguments("npx vite build"),
            moduleDir,
            repoRoot,
            stoppingToken).ConfigureAwait(false);

        stopwatch.Stop();

        if (success)
        {
            LogModuleRebuiltSuccessfully(logger, moduleName, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            LogModuleBuildFailed(logger, moduleName);
        }
    }

    private async Task RunClientAppBuild(
        string clientAppDir,
        string repoRoot,
        CancellationToken stoppingToken)
    {
        LogRebuildingClientApp(logger);
        var stopwatch = Stopwatch.StartNew();

        var success = await RunProcessAsync(
            GetShellFileName(),
            GetShellArguments("npx vite build"),
            clientAppDir,
            repoRoot,
            stoppingToken).ConfigureAwait(false);

        stopwatch.Stop();

        if (success)
        {
            LogClientAppRebuiltSuccessfully(logger, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            LogClientAppBuildFailed(logger);
        }
    }

    private async Task RunTailwindBuild(string repoRoot, CancellationToken stoppingToken)
    {
        LogRebuildingTailwind(logger);
        var stopwatch = Stopwatch.StartNew();

        var tailwindCli = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(repoRoot, "tools", "tailwindcss.exe")
            : Path.Combine(repoRoot, "tools", "tailwindcss");

        var hostDir = environment.ContentRootPath;
        var inputPath = Path.Combine(hostDir, "Styles", "app.css");
        var outputPath = Path.Combine(hostDir, "wwwroot", "css", "app.css");

        var command = $"\"{tailwindCli}\" -i \"{inputPath}\" -o \"{outputPath}\"";

        var success = await RunProcessAsync(
            GetShellFileName(),
            GetShellArguments(command),
            hostDir,
            repoRoot,
            stoppingToken).ConfigureAwait(false);

        stopwatch.Stop();

        if (success)
        {
            LogTailwindRebuiltSuccessfully(logger, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            LogTailwindBuildFailed(logger);
        }
    }

    private async Task<bool> RunProcessAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        string repoRoot,
        CancellationToken stoppingToken)
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

            // Set VITE_MODE=dev for dev builds
            process.StartInfo.Environment["VITE_MODE"] = "dev";

            // Ensure PATH includes node_modules/.bin from repo root
            var npmBinPath = Path.Combine(repoRoot, "node_modules", ".bin");
            var existingPath = process.StartInfo.Environment.TryGetValue("PATH", out var path) ? path : "";
            process.StartInfo.Environment["PATH"] = $"{npmBinPath}{Path.PathSeparator}{existingPath}";

            process.Start();

            // Read output streams asynchronously to avoid deadlocks
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

    private static string GetShellFileName()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "sh";
    }

    private static string GetShellArguments(string command)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"/c {command}" : $"-c \"{command}\"";
    }

    #region LoggerMessage definitions

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not find repository root (.git directory). ViteDevWatchService will not start.")]
    private static partial void LogRepoRootNotFound(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "ViteDevWatchService starting. Repo root: {RepoRoot}")]
    private static partial void LogServiceStarting(ILogger logger, string repoRoot);

    [LoggerMessage(Level = LogLevel.Information, Message = "ViteDevWatchService watching {ModuleCount} module(s), ClientApp, and Styles")]
    private static partial void LogWatchingStarted(ILogger logger, int moduleCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Module {ModuleName} file changed: {FilePath}")]
    private static partial void LogModuleFileChanged(ILogger logger, string moduleName, string? filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Watching module: {ModuleName} at {ModuleDir}")]
    private static partial void LogWatchingModule(ILogger logger, string moduleName, string moduleDir);

    [LoggerMessage(Level = LogLevel.Debug, Message = "ClientApp file changed: {FilePath}")]
    private static partial void LogClientAppFileChanged(ILogger logger, string? filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Watching ClientApp at {ClientAppDir}")]
    private static partial void LogWatchingClientApp(ILogger logger, string clientAppDir);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Styles file changed: {FilePath}")]
    private static partial void LogStylesFileChanged(ILogger logger, string? filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Watching Styles at {StylesDir}")]
    private static partial void LogWatchingStyles(ILogger logger, string stylesDir);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rebuilding module: {ModuleName}")]
    private static partial void LogRebuildingModule(ILogger logger, string moduleName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Module {ModuleName} rebuilt successfully in {ElapsedMs}ms")]
    private static partial void LogModuleRebuiltSuccessfully(ILogger logger, string moduleName, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Module {ModuleName} build failed")]
    private static partial void LogModuleBuildFailed(ILogger logger, string moduleName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rebuilding ClientApp")]
    private static partial void LogRebuildingClientApp(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "ClientApp rebuilt successfully in {ElapsedMs}ms")]
    private static partial void LogClientAppRebuiltSuccessfully(ILogger logger, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "ClientApp build failed")]
    private static partial void LogClientAppBuildFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rebuilding Tailwind CSS")]
    private static partial void LogRebuildingTailwind(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tailwind CSS rebuilt successfully in {ElapsedMs}ms")]
    private static partial void LogTailwindRebuiltSuccessfully(ILogger logger, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Tailwind CSS build failed")]
    private static partial void LogTailwindBuildFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Build stderr: {Stderr}")]
    private static partial void LogBuildStderr(ILogger logger, string stderr);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Build stdout: {Stdout}")]
    private static partial void LogBuildStdout(ILogger logger, string stdout);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Build output: {Stdout}")]
    private static partial void LogBuildOutput(ILogger logger, string stdout);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Build cancelled")]
    private static partial void LogBuildCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to run build process: {FileName} {Arguments}")]
    private static partial void LogBuildProcessFailed(ILogger logger, Exception ex, string fileName, string arguments);

    #endregion
}
