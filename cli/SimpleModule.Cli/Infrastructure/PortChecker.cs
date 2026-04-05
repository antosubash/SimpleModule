using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Cross-platform utility to check if a TCP port is in use and optionally
/// kill the process occupying it.
/// </summary>
public static class PortChecker
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

            // Verify port is now free
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

    /// <summary>
    /// Find the process listening on a given TCP port.
    /// Returns null if the port is free.
    /// </summary>
    public static PortBlocker? FindProcessOnPort(int port)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return FindOnWindows(port);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return FindWithLsof(port);
        }

        // Linux: try ss first (faster), fall back to lsof
        return FindWithSs(port) ?? FindWithLsof(port);
    }

    /// <summary>
    /// Linux: use `ss -tlnp` to find the listener.
    /// Output format: LISTEN 0 128 *:5001 *:* users:(("dotnet",pid=12345,fd=3))
    /// </summary>
    private static PortBlocker? FindWithSs(int port)
    {
        var output = RunCommand("ss", $"-tlnp sport = :{port}");
        if (output is null)
        {
            return null;
        }

        // Parse lines looking for pid=NNNN and the process name
        foreach (var line in output.Split('\n'))
        {
            if (!line.Contains($":{port}", StringComparison.Ordinal))
            {
                continue;
            }

            // Extract pid from users:(("name",pid=NNN,...))
            var pidIdx = line.IndexOf("pid=", StringComparison.Ordinal);
            if (pidIdx < 0)
            {
                continue;
            }

            var pidStart = pidIdx + 4;
            var pidEnd = line.IndexOfAny([',', ')'], pidStart);
            if (pidEnd < 0)
            {
                pidEnd = line.Length;
            }

            var pidStr = line[pidStart..pidEnd];
            if (
                !int.TryParse(
                    pidStr,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var pid
                )
            )
            {
                continue;
            }

            // Extract process name from (("name",...))
            var nameStart = line.IndexOf("((\"", StringComparison.Ordinal);
            var processName = "unknown";
            if (nameStart >= 0)
            {
                nameStart += 3;
                var nameEnd = line.IndexOf('"', nameStart);
                if (nameEnd > nameStart)
                {
                    processName = line[nameStart..nameEnd];
                }
            }

            return new PortBlocker(pid, processName);
        }

        return null;
    }

    /// <summary>
    /// macOS / Linux fallback: use `lsof -iTCP:PORT -sTCP:LISTEN -nP`.
    /// Output: dotnet  12345 user  3u  IPv6  0x...  0t0  TCP *:5001 (LISTEN)
    /// </summary>
    private static PortBlocker? FindWithLsof(int port)
    {
        var output = RunCommand("lsof", $"-iTCP:{port} -sTCP:LISTEN -nP -t");
        if (output is null)
        {
            return null;
        }

        // -t flag outputs just PIDs, one per line
        var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (
            firstLine is null
            || !int.TryParse(
                firstLine.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var pid
            )
        )
        {
            return null;
        }

        var processName = GetProcessName(pid) ?? "unknown";
        return new PortBlocker(pid, processName);
    }

    /// <summary>
    /// Windows: use `netstat -ano` to find the listener, then get process name.
    /// Output: TCP  0.0.0.0:5001  0.0.0.0:0  LISTENING  12345
    /// </summary>
    private static PortBlocker? FindOnWindows(int port)
    {
        var output = RunCommand("netstat", "-ano");
        if (output is null)
        {
            return null;
        }

        var portSuffix = $":{port}";
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (
                !trimmed.Contains("LISTENING", StringComparison.OrdinalIgnoreCase)
                || !trimmed.Contains(portSuffix, StringComparison.Ordinal)
            )
            {
                continue;
            }

            // Split by whitespace, last field is PID
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5)
            {
                continue;
            }

            // Verify the port is in the local address column (2nd field)
            if (!parts[1].EndsWith(portSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            var pidStr = parts[^1];
            if (
                !int.TryParse(
                    pidStr,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var pid
                )
            )
            {
                continue;
            }

            var processName = GetProcessName(pid) ?? "unknown";
            return new PortBlocker(pid, processName);
        }

        return null;
    }

    private static string? GetProcessName(int pid)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            return proc.ProcessName;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            return null;
        }
#pragma warning restore CA1031
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
