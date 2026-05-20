using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Maintenance;

public sealed class UpCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]Could not locate the host project. Run `sm up` from within a SimpleModule solution.[/]"
            );
            return 1;
        }

        var hostDir = Path.GetDirectoryName(solution.ApiCsprojPath);
        if (string.IsNullOrEmpty(hostDir))
        {
            AnsiConsole.MarkupLine("[red]Host project directory could not be resolved.[/]");
            return 1;
        }

        var removed = MaintenanceSentinel.Delete(hostDir);
        if (removed)
        {
            AnsiConsole.MarkupLine("[green]Maintenance mode disabled.[/] Sentinel removed.");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No active maintenance sentinel — nothing to do.[/]");
        }

        return 0;
    }
}
