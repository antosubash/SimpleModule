using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Maintenance;

public sealed class UpCommand : Command<UpSettings>
{
    public override int Execute(CommandContext context, UpSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]Could not find .slnx file. Run this command from within a SimpleModule project.[/]"
            );
            return 1;
        }

        var sentinelPath = MaintenanceSentinelFile.ResolvePath(solution);
        if (!File.Exists(sentinelPath))
        {
            AnsiConsole.MarkupLine("[green]Application is already up.[/]");
            return 0;
        }

        File.Delete(sentinelPath);
        AnsiConsole.MarkupLine("[green]Maintenance mode cleared. Application is up.[/]");
        return 0;
    }
}

public sealed class UpSettings : Spectre.Console.Cli.CommandSettings { }
