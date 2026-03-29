using System.Diagnostics;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Install;

public sealed class InstallCommand : Command<InstallSettings>
{
    public override int Execute(CommandContext context, InstallSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]No .slnx file found. Run this command from inside a SimpleModule project.[/]"
            );
            return 1;
        }

        var hostCsproj = solution.ApiCsprojPath;
        if (!File.Exists(hostCsproj))
        {
            AnsiConsole.MarkupLine(
                $"[red]Host project not found at {Markup.Escape(hostCsproj)}[/]"
            );
            return 1;
        }

        AnsiConsole.MarkupLine(
            $"Installing [green]{Markup.Escape(settings.PackageId)}[/] into [blue]{Markup.Escape(Path.GetFileName(hostCsproj))}[/]..."
        );

        var args = $"add \"{hostCsproj}\" package {settings.PackageId}";
        if (!string.IsNullOrWhiteSpace(settings.Version))
        {
            args += $" --version {settings.Version}";
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();

        if (process.ExitCode != 0)
        {
            AnsiConsole.MarkupLine("[red]Failed to install package.[/]");
            if (!string.IsNullOrWhiteSpace(error))
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(error.Trim())}[/]");
            }

            return 1;
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            AnsiConsole.WriteLine(output.Trim());
        }

        AnsiConsole.MarkupLine(
            $"[green]Successfully installed {Markup.Escape(settings.PackageId)}![/]"
        );
        AnsiConsole.MarkupLine("[dim]Run 'dotnet build' to discover the new module.[/]");

        return 0;
    }
}
