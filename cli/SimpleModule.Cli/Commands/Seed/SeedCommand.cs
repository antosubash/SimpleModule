using System.Diagnostics;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Seed;

public sealed class SeedCommand : Command<SeedSettings>
{
    public override int Execute(CommandContext context, SeedSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]Could not find .slnx file. Run this command from within a SimpleModule project.[/]"
            );
            return 1;
        }

        var seederProject = Path.Combine(
            solution.RootPath,
            "tools",
            "SimpleModule.PerfSeeder",
            "SimpleModule.PerfSeeder.csproj"
        );
        if (!File.Exists(seederProject))
        {
            AnsiConsole.MarkupLine(
                $"[red]Perf seeder project not found at {seederProject.EscapeMarkup()}[/]"
            );
            return 1;
        }

        var args = BuildForwardArgs(settings, solution);

        AnsiConsole.MarkupLine(
            "[bold blue]Running perf seeder[/] (this may take a while for large counts)..."
        );
        AnsiConsole.MarkupLine(
            $"[dim]  dotnet run -c Release --project {seederProject.EscapeMarkup()} -- {string.Join(' ', args).EscapeMarkup()}[/]"
        );

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = solution.RootPath,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(seederProject);
        startInfo.ArgumentList.Add("--");
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                AnsiConsole.MarkupLine("[red]Failed to start dotnet process.[/]");
                return 1;
            }
            process.WaitForExit();
            return process.ExitCode;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            AnsiConsole.MarkupLine($"[red]Seeder failed: {ex.Message.EscapeMarkup()}[/]");
            return 1;
        }
    }

    private static List<string> BuildForwardArgs(SeedSettings settings, SolutionContext solution)
    {
        var args = new List<string>
        {
            "--module",
            settings.Module,
            "--batch-size",
            settings.BatchSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--seed",
            settings.RandomSeed.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        if (settings.Count is { } count)
        {
            args.Add("--count");
            args.Add(count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        if (!string.IsNullOrWhiteSpace(settings.Connection))
        {
            args.Add("--connection");
            args.Add(settings.Connection);
        }
        if (!string.IsNullOrWhiteSpace(settings.Provider))
        {
            args.Add("--provider");
            args.Add(settings.Provider);
        }
        if (settings.Truncate)
        {
            args.Add("--truncate");
        }
        if (settings.CreateSchema)
        {
            args.Add("--create-schema");
        }

        // Always pass the host project so the seeder reads the right appsettings.json.
        var hostDir = Path.GetDirectoryName(solution.ApiCsprojPath);
        if (!string.IsNullOrWhiteSpace(hostDir))
        {
            args.Add("--project");
            args.Add(hostDir);
        }

        return args;
    }
}
