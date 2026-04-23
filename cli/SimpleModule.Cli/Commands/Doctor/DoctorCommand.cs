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

        AnsiConsole.Write(new Rule("[blue]Project health check[/]").LeftJustified());

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

        var fixedCount = 0;
        if (settings.Fix)
        {
            var beforeFail = results.Count(r => r.Status == CheckStatus.Fail);
            AutoFix(solution, results);
            results.Clear();
            foreach (var check in checks)
            {
                results.AddRange(check.Run(solution));
            }
            fixedCount = beforeFail - results.Count(r => r.Status == CheckStatus.Fail);
        }

        RenderResults(results);

        var failCount = results.Count(r => r.Status == CheckStatus.Fail);
        var warnCount = results.Count(r => r.Status == CheckStatus.Warning);
        var passCount = results.Count(r => r.Status == CheckStatus.Pass);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            BuildSummaryPanel(passCount, warnCount, failCount, fixedCount, settings.Fix)
        );

        return failCount > 0 ? 1 : 0;
    }

    private static void RenderResults(IReadOnlyList<CheckResult> results)
    {
        // Failures first so they're the first thing the user sees, then warnings,
        // then passes. Preserve discovery order within each status bucket.
        var ordered = results
            .Select((r, i) => (Result: r, Index: i))
            .OrderBy(x => StatusPriority(x.Result.Status))
            .ThenBy(x => x.Index)
            .Select(x => x.Result);

        var table = new Table().RoundedBorder();
        table.AddColumn("Status");
        table.AddColumn("Check");
        table.AddColumn("Details");

        foreach (var result in ordered)
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
    }

    private static int StatusPriority(CheckStatus status) =>
        status switch
        {
            CheckStatus.Fail => 0,
            CheckStatus.Warning => 1,
            CheckStatus.Pass => 2,
            _ => 3,
        };

    private static Panel BuildSummaryPanel(
        int passCount,
        int warnCount,
        int failCount,
        int fixedCount,
        bool fixRequested
    )
    {
        var total = passCount + warnCount + failCount;
        var lines = new List<string>
        {
            $"[green]{passCount} pass[/]  ·  [yellow]{warnCount} warn[/]  ·  [red]{failCount} fail[/]  [dim](of {total})[/]",
        };

        if (fixRequested && fixedCount > 0)
        {
            lines.Add($"[green]✓[/] Auto-fixed [green]{fixedCount}[/] issue(s)");
        }

        if (failCount > 0)
        {
            lines.Add(
                fixRequested
                    ? "[red]Some failures could not be auto-fixed.[/] See table above for details."
                    : "[dim]Run with --fix to auto-fix slnx entries, project references, Pages registry, and npm workspace globs.[/]"
            );
        }
        else if (warnCount > 0)
        {
            lines.Add("[green]All failures resolved.[/] Warnings are non-blocking.");
        }
        else
        {
            lines.Add("[green]All checks passed.[/]");
        }

        var color =
            failCount > 0 ? Color.Red
            : warnCount > 0 ? Color.Yellow
            : Color.Green;
        var header =
            failCount > 0 ? "Failing"
            : warnCount > 0 ? "Passing with warnings"
            : "Healthy";

        return new Panel(string.Join("\n", lines))
            .Header($"[bold]{header}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(color)
            .Expand();
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
                var moduleName = slashIndex >= 0 ? componentKey[..slashIndex] : componentKey;
                var featureName = slashIndex >= 0 ? componentKey[(slashIndex + 1)..] : componentKey;
                var indexPath = solution.GetModulePagesIndexPath(moduleName);
                PagesRegistryFixer.AddEntry(indexPath, componentKey, $"../Views/{featureName}");
                AnsiConsole.MarkupLine(
                    $"[green]  Fixed: added '{componentKey}' to Pages/index.ts[/]"
                );
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
