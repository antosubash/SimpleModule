using System.Diagnostics;
using System.Runtime.InteropServices;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Dev;

public sealed class DevCommand : Command<DevSettings>
{
    private readonly List<Process> _processes = [];
    private volatile bool _shuttingDown;

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

        // Wait for any process to exit
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
                _processes.Add(process);
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
        // Wait until any critical process exits
        while (!_shuttingDown)
        {
            foreach (var process in _processes)
            {
                if (process.HasExited)
                {
                    if (!_shuttingDown)
                    {
                        AnsiConsole.MarkupLine(
                            $"[yellow]Process exited with code {process.ExitCode}. Shutting down...[/]"
                        );
                        Shutdown();
                    }

                    return;
                }
            }

            Thread.Sleep(500);
        }
    }

    private void Shutdown()
    {
        if (_shuttingDown)
        {
            return;
        }

        _shuttingDown = true;
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[cyan]Stopping all processes...[/]");

        foreach (var process in _processes)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                // Best-effort kill
            }
#pragma warning restore CA1031
        }
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        Shutdown();
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        Shutdown();
    }
}
