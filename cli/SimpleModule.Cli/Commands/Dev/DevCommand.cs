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
    private volatile int _shutdownState; // ShutdownPhase values

    private static class ShutdownPhase
    {
        public const int Running = 0;
        public const int Graceful = 1;
        public const int Force = 2;
    }

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

        var startDotnet = !settings.NoDotnet;
        var startVite = !settings.NoVite && File.Exists(viteConfigPath);

        // --- Pre-flight: check all required ports before starting anything ---
        if (startDotnet)
        {
            var dotnetPorts = DiscoverDotnetPorts(hostProject);
            foreach (var port in dotnetPorts)
            {
                if (!PortChecker.EnsurePortFree(port, "dotnet"))
                {
                    AnsiConsole.MarkupLine(
                        $"[red]Cannot start dotnet — port {port} is occupied.[/]"
                    );
                    return 1;
                }
            }
        }

        if (startVite)
        {
            if (!PortChecker.EnsurePortFree(settings.VitePort, "vite"))
            {
                AnsiConsole.MarkupLine(
                    $"[red]Cannot start Vite — port {settings.VitePort} is occupied.[/]"
                );
                return 1;
            }
        }

        // --- Start processes ---
        if (startDotnet)
        {
            AnsiConsole.MarkupLine("[cyan][[dotnet]][/] Starting dotnet watch...");
            var dotnetArgs = $"watch run --project \"{hostProject}\" --no-restore";
            var dotnetEnv = new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
            };
            StartProcess("dotnet", dotnetArgs, solution.RootPath, "dotnet", dotnetEnv);
        }

        if (startVite)
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
        string label,
        IReadOnlyDictionary<string, string>? environment = null
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

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
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
        while (_shutdownState == ShutdownPhase.Running)
        {
            foreach (var (process, label) in _processes)
            {
                try
                {
                    if (process.HasExited)
                    {
                        if (_shutdownState == ShutdownPhase.Running)
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

    /// <summary>
    /// Parse launchSettings.json to find the ports ASP.NET will bind to.
    /// Falls back to the default ports (5001, 5000) if the file can't be read.
    /// </summary>
    private static List<int> DiscoverDotnetPorts(string hostProjectPath)
    {
        var ports = new List<int>();
        var hostDir = Path.GetDirectoryName(hostProjectPath);
        if (hostDir is null)
        {
            return [5001, 5000];
        }

        var launchSettingsPath = Path.Combine(hostDir, "Properties", "launchSettings.json");

        if (!File.Exists(launchSettingsPath))
        {
            return [5001, 5000];
        }

        try
        {
            var json = File.ReadAllText(launchSettingsPath);

            // Extract applicationUrl values and parse ports from them.
            // Format: "applicationUrl": "https://localhost:5001;http://localhost:5000"
            // Use simple string parsing to avoid adding a JSON dependency to the CLI.
            var searchKey = "\"applicationUrl\"";
            var idx = json.IndexOf(searchKey, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                var colonIdx = json.IndexOf(':', idx + searchKey.Length);
                if (colonIdx < 0)
                {
                    break;
                }

                var quoteStart = json.IndexOf('"', colonIdx + 1);
                if (quoteStart < 0)
                {
                    break;
                }

                var quoteEnd = json.IndexOf('"', quoteStart + 1);
                if (quoteEnd < 0)
                {
                    break;
                }

                var urlValue = json[(quoteStart + 1)..quoteEnd];
                foreach (var url in urlValue.Split(';'))
                {
                    // Extract port from URL like "https://localhost:5001"
                    var lastColon = url.LastIndexOf(':');
                    if (
                        lastColon >= 0
                        && int.TryParse(
                            url[(lastColon + 1)..],
                            System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var port
                        )
                    )
                    {
                        if (!ports.Contains(port))
                        {
                            ports.Add(port);
                        }
                    }
                }

                idx = json.IndexOf(searchKey, quoteEnd + 1, StringComparison.OrdinalIgnoreCase);
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            // If we can't parse, fall back to defaults
        }
#pragma warning restore CA1031

        return ports.Count > 0 ? ports : [5001, 5000];
    }
}
