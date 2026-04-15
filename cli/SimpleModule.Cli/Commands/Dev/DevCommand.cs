using System.Diagnostics;
using System.Runtime.InteropServices;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Dev;

public sealed partial class DevCommand : Command<DevSettings>
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
                $"vite --config \"{viteConfigPath}\" --port {settings.VitePort} --strictPort --configLoader runner";
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
}
