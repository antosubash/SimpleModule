using System.Diagnostics;
using Spectre.Console;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Cross-platform utility to check if a TCP port is in use and optionally
/// kill the process occupying it.
/// </summary>
public static partial class PortChecker
{
    /// <summary>
    /// Check if a port is in use. If it is, display the blocking process
    /// and ask the user whether to kill it.
    /// Returns true if the port is free (or was freed), false if still occupied.
    /// </summary>
    public static bool EnsurePortFree(int port, string serviceName)
    {
        var blocker = FindProcessOnPort(port);
        if (blocker is null)
        {
            return true;
        }

        AnsiConsole.MarkupLine(
            $"[yellow][[{serviceName}]][/] Port [bold]{port}[/] is already in use by "
                + $"[bold]{EscapeMarkup(blocker.Value.ProcessName)}[/] (PID {blocker.Value.Pid})"
        );

        var kill = AnsiConsole.Confirm(
            $"  Kill {EscapeMarkup(blocker.Value.ProcessName)} (PID {blocker.Value.Pid}) to free port {port}?",
            defaultValue: true
        );

        if (!kill)
        {
            AnsiConsole.MarkupLine(
                $"[yellow][[{serviceName}]][/] Port {port} still in use. Skipping."
            );
            return false;
        }

        if (KillProcess(blocker.Value.Pid))
        {
            // Wait briefly for the port to be released
            Thread.Sleep(500);

            var stillBlocked = FindProcessOnPort(port);
            if (stillBlocked is null)
            {
                AnsiConsole.MarkupLine(
                    $"[green][[{serviceName}]][/] Port {port} freed successfully."
                );
                return true;
            }

            AnsiConsole.MarkupLine(
                $"[red][[{serviceName}]][/] Port {port} still in use after kill. "
                    + $"Blocked by {EscapeMarkup(stillBlocked.Value.ProcessName)} (PID {stillBlocked.Value.Pid})."
            );
            return false;
        }

        AnsiConsole.MarkupLine(
            $"[red][[{serviceName}]][/] Failed to kill process {blocker.Value.Pid}."
        );
        return false;
    }

    private static bool KillProcess(int pid)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            proc.Kill(entireProcessTree: true);
            proc.WaitForExit(3000);
            return proc.HasExited;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            return false;
        }
#pragma warning restore CA1031
    }

    private static string? RunCommand(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            );

            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return output;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }

    private static string EscapeMarkup(string text)
    {
        return text.Replace("[", "[[", StringComparison.Ordinal)
            .Replace("]", "]]", StringComparison.Ordinal);
    }
}

public readonly record struct PortBlocker(int Pid, string ProcessName);
