using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SimpleModule.Cli.Infrastructure;

public static partial class PortChecker
{
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

        foreach (var line in output.Split('\n'))
        {
            if (!line.Contains($":{port}", StringComparison.Ordinal))
            {
                continue;
            }

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

            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5)
            {
                continue;
            }

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
}
