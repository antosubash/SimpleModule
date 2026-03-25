# Vite Dev Watch Service Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make `dotnet run` the single command for development by adding a .NET BackgroundService that watches frontend files and triggers targeted Vite rebuilds.

**Architecture:** A `ViteDevWatchService : BackgroundService` in the Host project discovers all modules with `vite.config.ts`, sets up `FileSystemWatcher` instances, and runs `npx vite build` on-demand when files change. Debounced at 300ms, one build at a time per module.

**Tech Stack:** .NET BackgroundService, FileSystemWatcher, Process (for npx), ILogger

---

### Task 1: Create ViteDevWatchService

**Files:**
- Create: `template/SimpleModule.Host/Services/ViteDevWatchService.cs`

**Step 1: Create the Services directory and service file**

```csharp
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SimpleModule.Host.Services;

public sealed class ViteDevWatchService(
    ILogger<ViteDevWatchService> logger,
    IHostEnvironment environment
) : BackgroundService
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(300);

    private static readonly string[] WatchedExtensions = [".ts", ".tsx", ".css", ".jsx", ".js"];

    private readonly ConcurrentDictionary<string, DateTime> _lastChangeTime = new();
    private readonly ConcurrentDictionary<string, bool> _buildInProgress = new();
    private readonly List<FileSystemWatcher> _watchers = [];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var repoRoot = FindRepoRoot(environment.ContentRootPath);
        if (repoRoot is null)
        {
            logger.LogWarning("Could not find repository root from {ContentRoot}. Vite watch disabled.", environment.ContentRootPath);
            return Task.CompletedTask;
        }

        var modules = DiscoverModulesWithVite(repoRoot);
        logger.LogInformation("Vite watch: discovered {Count} modules", modules.Count);

        foreach (var (name, modulePath) in modules)
        {
            SetupWatcher(name, modulePath, repoRoot, stoppingToken);
        }

        // Watch ClientApp
        var clientAppPath = Path.Combine(repoRoot, "template", "SimpleModule.Host", "ClientApp");
        if (Directory.Exists(clientAppPath))
        {
            SetupWatcher("ClientApp", clientAppPath, repoRoot, stoppingToken);
        }

        // Watch Tailwind input CSS
        var tailwindInput = Path.Combine(repoRoot, "template", "SimpleModule.Host", "Styles");
        if (Directory.Exists(tailwindInput))
        {
            SetupTailwindWatcher(tailwindInput, repoRoot, stoppingToken);
        }

        return Task.CompletedTask;
    }

    private void SetupWatcher(string name, string watchPath, string repoRoot, CancellationToken ct)
    {
        var watcher = new FileSystemWatcher(watchPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true,
        };

        foreach (var ext in WatchedExtensions)
        {
            watcher.Filters.Add($"*{ext}");
        }

        watcher.Changed += (_, e) => OnFileChanged(name, watchPath, e.FullPath, ct);
        watcher.Created += (_, e) => OnFileChanged(name, watchPath, e.FullPath, ct);
        watcher.Renamed += (_, e) => OnFileChanged(name, watchPath, e.FullPath, ct);

        _watchers.Add(watcher);
        logger.LogInformation("Vite watch: watching {Name} at {Path}", name, watchPath);
    }

    private void SetupTailwindWatcher(string stylesPath, string repoRoot, CancellationToken ct)
    {
        var watcher = new FileSystemWatcher(stylesPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            Filter = "*.css",
            EnableRaisingEvents = true,
        };

        watcher.Changed += (_, e) => OnTailwindFileChanged(repoRoot, e.FullPath, ct);
        watcher.Created += (_, e) => OnTailwindFileChanged(repoRoot, e.FullPath, ct);

        _watchers.Add(watcher);
        logger.LogInformation("Vite watch: watching Tailwind styles at {Path}", stylesPath);
    }

    private void OnFileChanged(string name, string workingDir, string filePath, CancellationToken ct)
    {
        // Ignore wwwroot (build output) and node_modules
        if (filePath.Contains("wwwroot", StringComparison.OrdinalIgnoreCase)
            || filePath.Contains("node_modules", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _lastChangeTime[name] = DateTime.UtcNow;

        _ = Task.Run(async () =>
        {
            await Task.Delay(DebounceDelay, ct);

            // If another change came in during debounce, skip this one
            if (_lastChangeTime.TryGetValue(name, out var lastChange)
                && (DateTime.UtcNow - lastChange) < DebounceDelay)
            {
                return;
            }

            // If build already in progress, skip
            if (!_buildInProgress.TryAdd(name, true))
            {
                return;
            }

            try
            {
                logger.LogInformation("Vite watch: rebuilding {Name}...", name);
                var sw = Stopwatch.StartNew();

                await RunViteBuild(workingDir, ct);

                sw.Stop();
                logger.LogInformation("Vite watch: {Name} rebuilt in {Elapsed}ms", name, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Shutting down, ignore
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Vite watch: failed to rebuild {Name}", name);
            }
            finally
            {
                _buildInProgress.TryRemove(name, out _);
            }
        }, ct);
    }

    private void OnTailwindFileChanged(string repoRoot, string filePath, CancellationToken ct)
    {
        // Ignore _scan directory (build output)
        if (filePath.Contains("_scan", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        const string name = "Tailwind";
        _lastChangeTime[name] = DateTime.UtcNow;

        _ = Task.Run(async () =>
        {
            await Task.Delay(DebounceDelay, ct);

            if (_lastChangeTime.TryGetValue(name, out var lastChange)
                && (DateTime.UtcNow - lastChange) < DebounceDelay)
            {
                return;
            }

            if (!_buildInProgress.TryAdd(name, true))
            {
                return;
            }

            try
            {
                logger.LogInformation("Vite watch: rebuilding Tailwind...");
                var sw = Stopwatch.StartNew();

                await RunTailwindBuild(repoRoot, ct);

                sw.Stop();
                logger.LogInformation("Vite watch: Tailwind rebuilt in {Elapsed}ms", name, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Shutting down
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Vite watch: failed to rebuild Tailwind");
            }
            finally
            {
                _buildInProgress.TryRemove(name, out _);
            }
        }, ct);
    }

    private static async Task RunViteBuild(string workingDir, CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd" : "sh",
            Arguments = OperatingSystem.IsWindows() ? "/c npx vite build" : "-c \"npx vite build\"",
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Environment = { ["VITE_MODE"] = "dev" },
        };

        process.Start();
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            throw new InvalidOperationException($"Vite build failed (exit code {process.ExitCode}): {stderr}");
        }
    }

    private static async Task RunTailwindBuild(string repoRoot, CancellationToken ct)
    {
        var tailwindCli = OperatingSystem.IsWindows()
            ? Path.Combine(repoRoot, "tools", "tailwindcss.exe")
            : Path.Combine(repoRoot, "tools", "tailwindcss");

        var hostDir = Path.Combine(repoRoot, "template", "SimpleModule.Host");
        var input = Path.Combine(hostDir, "Styles", "app.css");
        var output = Path.Combine(hostDir, "wwwroot", "css", "app.css");

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = tailwindCli,
            Arguments = $"-i \"{input}\" -o \"{output}\"",
            WorkingDirectory = hostDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        process.Start();
        await process.WaitForExitAsync(ct);
    }

    private static List<(string Name, string Path)> DiscoverModulesWithVite(string repoRoot)
    {
        var modules = new List<(string, string)>();
        var modulesDir = Path.Combine(repoRoot, "modules");

        if (!Directory.Exists(modulesDir))
        {
            return modules;
        }

        foreach (var groupDir in Directory.GetDirectories(modulesDir))
        {
            var srcDir = Path.Combine(groupDir, "src");
            if (!Directory.Exists(srcDir))
            {
                continue;
            }

            foreach (var moduleDir in Directory.GetDirectories(srcDir))
            {
                var viteConfig = Path.Combine(moduleDir, "vite.config.ts");
                var packageJson = Path.Combine(moduleDir, "package.json");

                if (File.Exists(viteConfig) && File.Exists(packageJson))
                {
                    var name = new DirectoryInfo(moduleDir).Name;
                    modules.Add((name, moduleDir));
                }
            }
        }

        return modules.OrderBy(m => m.Name).ToList();
    }

    private static string? FindRepoRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    public override void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _watchers.Clear();
        base.Dispose();
    }
}
```

**Step 2: Verify the file compiles**

Run: `dotnet build template/SimpleModule.Host`
Expected: Build succeeds (service exists but isn't registered yet)

**Step 3: Commit**

```bash
git add template/SimpleModule.Host/Services/ViteDevWatchService.cs
git commit -m "feat: add ViteDevWatchService for .NET-native frontend file watching"
```

---

### Task 2: Register ViteDevWatchService in Program.cs

**Files:**
- Modify: `template/SimpleModule.Host/Program.cs:57-59`

**Step 1: Add the service registration after `AddModuleSystem`**

After line 59 (`builder.Services.AddModuleHealthChecks();`), add:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<SimpleModule.Host.Services.ViteDevWatchService>();
}
```

**Step 2: Build and verify**

Run: `dotnet build template/SimpleModule.Host`
Expected: Build succeeds

**Step 3: Run and verify watcher starts**

Run: `dotnet run --project template/SimpleModule.Host`
Expected: Console shows log lines like:
- `Vite watch: discovered 9 modules`
- `Vite watch: watching Products at ...`
- `Vite watch: watching ClientApp at ...`
- `Vite watch: watching Tailwind styles at ...`

**Step 4: Manual test — edit a module file and verify rebuild**

1. Edit `modules/Products/src/Products/Pages/index.ts` (add a blank line)
2. Watch console for: `Vite watch: rebuilding Products...`
3. Wait for: `Vite watch: Products rebuilt in XXXms`

**Step 5: Commit**

```bash
git add template/SimpleModule.Host/Program.cs
git commit -m "feat: register ViteDevWatchService in development mode"
```

---

### Task 3: Verify end-to-end and clean up

**Step 1: Full clean build**

Run: `dotnet build template/SimpleModule.Host`
Expected: All Vite modules build via MSBuild JsBuild targets, build succeeds

**Step 2: Run with `dotnet run` only**

Run: `dotnet run --project template/SimpleModule.Host`
Expected:
- App starts on https://localhost:5001
- Watcher log lines appear
- Editing any `.tsx` file in a module triggers a rebuild log

**Step 3: Verify graceful shutdown**

Press Ctrl+C in the terminal.
Expected: App shuts down cleanly, no orphaned processes.

**Step 4: Verify `npm run dev` still works as fallback**

Run: `npm run dev`
Expected: Still works (we didn't remove anything)
