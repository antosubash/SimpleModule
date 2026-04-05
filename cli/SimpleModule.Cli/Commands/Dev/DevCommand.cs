using System.Diagnostics;
using System.Runtime.InteropServices;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Dev;

public sealed class DevCommand : Command<DevSettings>
{
    /// <summary>How long to wait for graceful shutdown before force-killing.</summary>
    private const int GracefulShutdownTimeoutMs = 5000;

    /// <summary>How long to wait after force-kill before giving up.</summary>
    private const int ForceKillTimeoutMs = 3000;

    private readonly List<(Process Process, string Label)> _processes = [];
    private volatile int _shutdownState; // 0=running, 1=graceful, 2=force

    public override int Execute(CommandContext context, DevSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]Could not find .slnx file. Run this command from within a SimpleModule project.[/]"
            );
            return 1;
        }

        var hostProject = solution.ApiCsprojPath;
        if (!File.Exists(hostProject))
        {
            AnsiConsole.MarkupLine($"[red]Host project not found at {hostProject}[/]");
            return 1;
        }

        var hostDir = Path.GetDirectoryName(hostProject)!;
        var clientAppDir = Path.Combine(hostDir, "ClientApp");
        var viteConfigPath = Path.Combine(clientAppDir, "vite.dev.config.ts");

        Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        try
        {
            return Run(settings, solution, hostProject, viteConfigPath);
        }
        finally
        {
            // Always clean up — even on unhandled exceptions
            ForceKillAll();
            DisposeAll();
            Console.CancelKeyPress -= OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        }
    }

    private int Run(
        DevSettings settings,
        SolutionContext solution,
        string hostProject,
        string viteConfigPath
    )
    {
        AnsiConsole.MarkupLine("[bold blue]Starting SimpleModule development environment[/]");
        AnsiConsole.MarkupLine("");

        // Start dotnet watch (hot reload for C# changes)
        if (!settings.NoDotnet)
        {
            AnsiConsole.MarkupLine("[cyan][[dotnet]][/] Starting dotnet watch...");
            var dotnetArgs = $"watch run --project \"{hostProject}\" --no-restore";
            StartProcess("dotnet", dotnetArgs, solution.RootPath, "dotnet");
        }

        // Start Vite dev server (HMR for frontend changes)
        if (!settings.NoVite && File.Exists(viteConfigPath))
        {
            AnsiConsole.MarkupLine(
                $"[cyan][[vite]][/] Starting Vite dev server on port {settings.VitePort}..."
            );
            var npx = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "npx.cmd" : "npx";
            var viteArgs =
                $"vite dev --config \"{viteConfigPath}\" --port {settings.VitePort} --strictPort";
            StartProcess(npx, viteArgs, solution.RootPath, "vite");
        }
        else if (!settings.NoVite && !File.Exists(viteConfigPath))
        {
            AnsiConsole.MarkupLine(
                "[yellow][[vite]][/] vite.dev.config.ts not found in ClientApp — skipping Vite dev server"
            );
        }

        if (_processes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No processes started. Check your options.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine(
            $"[green]Development environment running ({_processes.Count} process(es)). Press Ctrl+C to stop.[/]"
        );

        WaitForExit();
        return 0;
    }

    private void StartProcess(
        string fileName,
        string arguments,
        string workingDirectory,
        string label
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
        };

        // Ensure ASPNETCORE_ENVIRONMENT is set for dotnet
        if (label == "dotnet")
        {
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        }

        try
        {
            var process = Process.Start(startInfo);
            if (process is not null)
            {
                _processes.Add((process, label));
                AnsiConsole.MarkupLine($"[dim][[{label}]][/] [dim]Started (PID {process.Id})[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red][[{label}]][/] Failed to start process");
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            AnsiConsole.MarkupLine($"[red][[{label}]][/] Error: {ex.Message}");
        }
    }

    private void WaitForExit()
    {
        // Wait until any process exits or shutdown is requested
        while (_shutdownState == 0)
        {
            foreach (var (process, label) in _processes)
            {
                try
                {
                    if (process.HasExited)
                    {
                        if (_shutdownState == 0)
                        {
                            AnsiConsole.MarkupLine(
                                $"[yellow][[{label}]][/] Exited with code {process.ExitCode}. Shutting down..."
                            );
                            GracefulShutdown();
                        }

                        return;
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
                {
                    // Process may have been disposed between check and access
                }
#pragma warning restore CA1031
            }

            Thread.Sleep(300);
        }
    }

    /// <summary>
    /// Graceful shutdown: send SIGTERM/SIGINT to children, wait for them to exit,
    /// then force-kill any stragglers.
    /// </summary>
    private void GracefulShutdown()
    {
        // Transition: running → graceful
        if (Interlocked.CompareExchange(ref _shutdownState, 1, 0) != 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[cyan]Stopping all processes gracefully...[/]");

        // Phase 1: Send graceful termination signal
        foreach (var (process, label) in _processes)
        {
            SendTermSignal(process, label);
        }

        // Phase 2: Wait for graceful exit
        if (WaitAllExit(GracefulShutdownTimeoutMs))
        {
            AnsiConsole.MarkupLine("[green]All processes stopped.[/]");
            return;
        }

        // Phase 3: Force-kill survivors
        AnsiConsole.MarkupLine(
            "[yellow]Some processes did not exit gracefully. Force-killing...[/]"
        );
        ForceKillAll();

        if (!WaitAllExit(ForceKillTimeoutMs))
        {
            AnsiConsole.MarkupLine(
                "[red]Warning: Some processes may still be running. Check manually.[/]"
            );
            LogSurvivorPids();
        }
        else
        {
            AnsiConsole.MarkupLine("[green]All processes stopped.[/]");
        }
    }

    /// <summary>
    /// Send SIGTERM on Linux/macOS or Kill on Windows to a single process tree.
    /// </summary>
    private static void SendTermSignal(Process process, string label)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows has no graceful signal — use taskkill /T (tree)
                // which sends WM_CLOSE to console apps
                using var taskkill = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/PID {process.Id} /T",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    }
                );
                taskkill?.WaitForExit(3000);
            }
            else
            {
                // Unix: send SIGTERM to the process group (negative PID)
                // This catches child processes spawned by shell wrappers like npx
                var killPgid = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "kill",
                        Arguments = $"-TERM -{process.Id}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    }
                );
                killPgid?.WaitForExit(1000);

                // Also send SIGTERM directly to the process (in case it's not a group leader)
                if (!process.HasExited)
                {
                    var killPid = Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "kill",
                            Arguments = $"-TERM {process.Id}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                        }
                    );
                    killPid?.WaitForExit(1000);
                }
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            AnsiConsole.MarkupLine(
                $"[dim][[{label}]][/] [dim]Failed to send term signal: {ex.Message}[/]"
            );
        }
    }

    /// <summary>
    /// Force-kill all processes and their entire process trees.
    /// </summary>
    private void ForceKillAll()
    {
        // Transition to force state (from any state)
        Interlocked.Exchange(ref _shutdownState, 2);

        foreach (var (process, label) in _processes)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031
            {
                // Kill can fail if process exited between check and kill,
                // or if we lack permissions for a child process
                AnsiConsole.MarkupLine(
                    $"[dim][[{label}]][/] [dim]Force-kill failed (PID {GetSafePid(process)}): {ex.Message}[/]"
                );
            }
        }
    }

    /// <summary>
    /// Wait for all tracked processes to exit within the given timeout.
    /// Returns true if all exited, false if any are still alive.
    /// </summary>
    private bool WaitAllExit(int timeoutMs)
    {
        var deadline = Environment.TickCount64 + timeoutMs;

        foreach (var (process, _) in _processes)
        {
            var remaining = (int)(deadline - Environment.TickCount64);
            if (remaining <= 0)
            {
                return AllExited();
            }

            try
            {
                process.WaitForExit(remaining);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                // Process may already be disposed
            }
#pragma warning restore CA1031
        }

        return AllExited();
    }

    private bool AllExited()
    {
        foreach (var (process, _) in _processes)
        {
            try
            {
                if (!process.HasExited)
                {
                    return false;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                // Process disposed — treat as exited
            }
#pragma warning restore CA1031
        }

        return true;
    }

    private void LogSurvivorPids()
    {
        foreach (var (process, label) in _processes)
        {
            try
            {
                if (!process.HasExited)
                {
                    AnsiConsole.MarkupLine($"[red][[{label}]][/] Still running: PID {process.Id}");
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                // Ignore
            }
#pragma warning restore CA1031
        }
    }

    private void DisposeAll()
    {
        foreach (var (process, _) in _processes)
        {
            try
            {
                process.Dispose();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                // Best-effort dispose
            }
#pragma warning restore CA1031
        }

        _processes.Clear();
    }

    private static int GetSafePid(Process process)
    {
        try
        {
            return process.Id;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            return -1;
        }
#pragma warning restore CA1031
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        if (_shutdownState == 0)
        {
            // First Ctrl+C: graceful shutdown, cancel the default termination
            e.Cancel = true;
            GracefulShutdown();
        }
        else
        {
            // Second Ctrl+C: force-kill immediately, let process terminate
            AnsiConsole.MarkupLine("[red]Force-killing all processes...[/]");
            ForceKillAll();
            e.Cancel = false;
        }
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        // Process is exiting (terminal closed, kill signal, etc.)
        // Force-kill children to prevent orphans
        ForceKillAll();
    }
}
