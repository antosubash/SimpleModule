using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace SimpleModule.Cli.Commands.Dev;

public sealed partial class DevCommand
{
    /// <summary>
    /// Send a graceful termination signal to a process and all its descendants.
    /// <para>
    /// <b>Linux/macOS:</b> Enumerates the process tree via the system process list
    /// and sends SIGTERM to each descendant (leaf-first) then to the root.
    /// Cannot use <c>kill -TERM -pgid</c> because children started with
    /// <c>UseShellExecute=false</c> inherit the parent's process group.
    /// </para>
    /// <para>
    /// <b>Windows:</b> Uses <c>taskkill /PID &lt;pid&gt; /T</c> which sends
    /// WM_CLOSE to the entire process tree.
    /// </para>
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
                // taskkill /T walks the process tree and sends WM_CLOSE (graceful)
                // /F is intentionally omitted — we want graceful first
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
                // Collect all descendant PIDs first, then signal leaf-first so
                // parent processes don't respawn children before we reach them.
                var descendants = GetDescendantPids(process.Id);
                descendants.Reverse(); // leaf-first order

                foreach (var pid in descendants)
                {
                    SendSigterm(pid);
                }

                // Finally signal the root process itself
                SendSigterm(process.Id);
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
    /// Send SIGTERM to a single PID via the <c>kill</c> command.
    /// Silently ignores errors (process may have already exited).
    /// </summary>
    private static void SendSigterm(int pid)
    {
        try
        {
            using var kill = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "kill",
                    Arguments = $"-TERM {pid}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            );
            kill?.WaitForExit(1000);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            // Process may have already exited
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Get all descendant PIDs of a process by walking the process tree.
    /// Works on both Linux (<c>/proc</c>) and macOS (<c>pgrep -P</c>).
    /// Returns PIDs in breadth-first order (parents before children).
    /// </summary>
    private static List<int> GetDescendantPids(int rootPid)
    {
        var descendants = new List<int>();
        var queue = new Queue<int>();
        queue.Enqueue(rootPid);

        while (queue.Count > 0)
        {
            var parentPid = queue.Dequeue();
            List<int> children;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                children = GetChildPidsFromProc(parentPid);
            }
            else
            {
                // macOS (and other Unix): use pgrep -P <pid>
                children = GetChildPidsViaPgrep(parentPid);
            }

            foreach (var childPid in children)
            {
                descendants.Add(childPid);
                queue.Enqueue(childPid);
            }
        }

        return descendants;
    }

    /// <summary>
    /// Linux: read /proc to find child PIDs. Each /proc/[pid]/stat has ppid as field 4.
    /// </summary>
    private static List<int> GetChildPidsFromProc(int parentPid)
    {
        var children = new List<int>();

        try
        {
            foreach (var dir in Directory.GetDirectories("/proc"))
            {
                var dirName = Path.GetFileName(dir);
                if (!int.TryParse(dirName, out var pid))
                {
                    continue;
                }

                try
                {
                    var stat = File.ReadAllText(Path.Combine(dir, "stat"));
                    // Format: pid (comm) state ppid ...
                    // Find the closing ')' to skip the command name (which can contain spaces)
                    var closeParen = stat.LastIndexOf(')');
                    if (closeParen < 0)
                    {
                        continue;
                    }

                    var fields = stat[(closeParen + 2)..].Split(' ');
                    // fields[0] = state, fields[1] = ppid
                    if (
                        fields.Length > 1
                        && int.TryParse(fields[1], out var ppid)
                        && ppid == parentPid
                    )
                    {
                        children.Add(pid);
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
                {
                    // Process may have exited between directory listing and read
                }
#pragma warning restore CA1031
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            // /proc not available or permission denied
        }
#pragma warning restore CA1031

        return children;
    }

    /// <summary>
    /// macOS/Unix: use <c>pgrep -P &lt;pid&gt;</c> to find child PIDs.
    /// </summary>
    private static List<int> GetChildPidsViaPgrep(int parentPid)
    {
        var children = new List<int>();

        try
        {
            using var pgrep = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "pgrep",
                    Arguments = $"-P {parentPid}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            );

            if (pgrep is null)
            {
                return children;
            }

            var output = pgrep.StandardOutput.ReadToEnd();
            pgrep.WaitForExit(2000);

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(line.Trim(), out var pid))
                {
                    children.Add(pid);
                }
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            // pgrep not available
        }
#pragma warning restore CA1031

        return children;
    }
}
