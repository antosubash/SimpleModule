using Spectre.Console;

namespace SimpleModule.Cli.Commands.Dev;

public sealed partial class DevCommand
{
    /// <summary>
    /// Graceful shutdown: send termination signals to children, wait for them to exit,
    /// then force-kill any stragglers.
    /// </summary>
    private void GracefulShutdown()
    {
        // Transition: running → graceful
        if (
            Interlocked.CompareExchange(
                ref _shutdownState,
                ShutdownPhase.Graceful,
                ShutdownPhase.Running
            ) != ShutdownPhase.Running
        )
        {
            return;
        }

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[cyan]Stopping all processes gracefully...[/]");

        // Phase 1: Send graceful termination signal to the entire process tree
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
    /// Force-kill all processes and their entire process trees.
    /// Uses .NET's cross-platform <c>Kill(entireProcessTree: true)</c> which
    /// walks /proc on Linux, libproc on macOS, and NtQuerySystemInformation on Windows.
    /// </summary>
    private void ForceKillAll()
    {
        // Transition to force state (from any state)
        Interlocked.Exchange(ref _shutdownState, ShutdownPhase.Force);

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
                // or if we lack permissions for a child process.
                // Fall back to killing just the direct process.
                AnsiConsole.MarkupLine(
                    $"[dim][[{label}]][/] [dim]Tree kill failed (PID {GetSafePid(process)}): {ex.Message}[/]"
                );
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: false);
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
                {
                    // Truly unreachable
                }
#pragma warning restore CA1031
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

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        if (_shutdownState == ShutdownPhase.Running)
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
