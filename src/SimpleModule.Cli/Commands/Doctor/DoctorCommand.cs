using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Doctor;

public sealed class DoctorCommand : Command<DoctorSettings>
{
    public override int Execute(CommandContext context, DoctorSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]Could not find .slnx file. Run this command from within a SimpleModule project.[/]"
            );
            return 1;
        }

        AnsiConsole.MarkupLine("[blue]Running project health checks...[/]");
        AnsiConsole.MarkupLine("");

        IDoctorCheck[] checks =
        [
            new SolutionStructureCheck(),
            new ProjectReferenceCheck(),
            new SlnxEntriesCheck(),
            new CsprojConventionCheck(),
            new ModulePatternCheck(),
        ];

        var results = new List<CheckResult>();
        foreach (var check in checks)
        {
            results.AddRange(check.Run(solution));
        }

        // Auto-fix if requested
        if (settings.Fix)
        {
            AutoFix(solution, results);
            // Re-run checks after fix
            results.Clear();
            foreach (var check in checks)
            {
                results.AddRange(check.Run(solution));
            }
        }

        // Display results table
        var table = new Table();
        table.AddColumn("Status");
        table.AddColumn("Check");
        table.AddColumn("Details");

        foreach (var result in results)
        {
            var statusMarkup = result.Status switch
            {
                CheckStatus.Pass => "[green]PASS[/]",
                CheckStatus.Warning => "[yellow]WARN[/]",
                CheckStatus.Fail => "[red]FAIL[/]",
                _ => "[dim]?[/]",
            };
            table.AddRow(statusMarkup, Markup.Escape(result.Name), Markup.Escape(result.Message));
        }

        AnsiConsole.Write(table);

        var failCount = results.Count(r => r.Status == CheckStatus.Fail);
        var warnCount = results.Count(r => r.Status == CheckStatus.Warning);

        AnsiConsole.MarkupLine("");
        if (failCount > 0)
        {
            AnsiConsole.MarkupLine(
                $"[red]{failCount} failure(s)[/], [yellow]{warnCount} warning(s)[/]"
            );
            if (!settings.Fix)
            {
                AnsiConsole.MarkupLine(
                    "[dim]Run with --fix to auto-fix missing slnx entries and project references.[/]"
                );
            }

            return 1;
        }

        if (warnCount > 0)
        {
            AnsiConsole.MarkupLine(
                $"[green]All checks passed[/] with [yellow]{warnCount} warning(s)[/]"
            );
        }
        else
        {
            AnsiConsole.MarkupLine("[green]All checks passed![/]");
        }

        return 0;
    }

    private static void AutoFix(SolutionContext solution, List<CheckResult> results)
    {
        AnsiConsole.MarkupLine("[blue]Attempting auto-fix...[/]");

        foreach (var result in results.Where(r => r.Status == CheckStatus.Fail))
        {
            // Fix missing slnx entries
            if (result.Name.StartsWith("Slnx -> ", StringComparison.Ordinal))
            {
                var moduleName = result.Name["Slnx -> ".Length..];
                SlnxManipulator.AddModuleEntries(solution.SlnxPath, moduleName);
                AnsiConsole.MarkupLine($"[green]  Fixed: added {moduleName} to .slnx[/]");
            }

            // Fix missing project references
            if (result.Name.StartsWith("API -> ", StringComparison.Ordinal))
            {
                var moduleName = result.Name["API -> ".Length..];
                ProjectManipulator.AddProjectReference(
                    solution.ApiCsprojPath,
                    $@"..\modules\{moduleName}\{moduleName}\{moduleName}.csproj"
                );
                AnsiConsole.MarkupLine(
                    $"[green]  Fixed: added {moduleName} reference to API csproj[/]"
                );
            }
        }

        AnsiConsole.MarkupLine("");
    }
}
