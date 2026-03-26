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
            new ContractsIsolationCheck(),
            new ModulePatternCheck(),
            new ModuleAttributeCheck(),
            new ViewEndpointNamingCheck(),
            new PagesRegistryCheck(),
            new ViteConfigCheck(),
            new PackageJsonCheck(),
            new NpmWorkspaceCheck(),
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
                    "[dim]Run with --fix to auto-fix missing slnx entries, project references, Pages registry entries, and npm workspace globs.[/]"
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

            // Fix missing Pages/index.ts entries
            if (result.Name.StartsWith("Pages -> ", StringComparison.Ordinal))
            {
                var componentKey = result.Name["Pages -> ".Length..];
                var slashIndex = componentKey.IndexOf('/', StringComparison.Ordinal);
                var moduleName = slashIndex >= 0
                    ? componentKey[..slashIndex]
                    : componentKey;
                var featureName = slashIndex >= 0
                    ? componentKey[(slashIndex + 1)..]
                    : componentKey;
                var indexPath = solution.GetModulePagesIndexPath(moduleName);
                PagesRegistryFixer.AddEntry(indexPath, componentKey, $"../Views/{featureName}");
                AnsiConsole.MarkupLine($"[green]  Fixed: added '{componentKey}' to Pages/index.ts[/]");
            }

            // Fix missing npm workspace entries
            if (result.Name.StartsWith("NpmWorkspace -> ", StringComparison.Ordinal))
            {
                var moduleName = result.Name["NpmWorkspace -> ".Length..];
                var rootPackageJson = Path.Combine(solution.RootPath, "package.json");
                if (File.Exists(rootPackageJson))
                {
                    NpmWorkspaceFixer.AddWorkspaceGlob(
                        rootPackageJson,
                        $"src/modules/{moduleName}/src/*"
                    );
                    AnsiConsole.MarkupLine(
                        $"[green]  Fixed: added {moduleName} workspace glob to package.json[/]"
                    );
                }
            }
        }

        AnsiConsole.MarkupLine("");
    }
}
