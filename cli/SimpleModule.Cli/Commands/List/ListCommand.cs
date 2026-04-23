using System.Text.RegularExpressions;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.List;

public sealed partial class ListCommand : Command<ListSettings>
{
    [GeneratedRegex(
        """RoutePrefix\s*=\s*(?:"(?<literal>[^"]*)"|(?<constref>[A-Za-z_][\w\.]*))""",
        RegexOptions.Singleline,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex RoutePrefixRegex();

    [GeneratedRegex(
        """public\s+const\s+string\s+RoutePrefix\s*=\s*"(?<value>[^"]*)"\s*;""",
        RegexOptions.Singleline,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex ConstantsRoutePrefixRegex();

    public override int Execute(CommandContext context, ListSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]No .slnx file found. Run this command from inside a SimpleModule project.[/]"
            );
            return 1;
        }

        if (solution.ExistingModules.Count == 0)
        {
            AnsiConsole.MarkupLine(
                "[yellow]No modules found.[/] Create one with [green]sm new module <Name>[/]."
            );
            return 0;
        }

        var table = new Table().RoundedBorder();
        table.AddColumn("Module");
        table.AddColumn("Route prefix");
        table.AddColumn(new TableColumn("Endpoints").RightAligned());

        foreach (var module in solution.ExistingModules)
        {
            var routePrefix = ReadRoutePrefix(solution, module);
            var endpointCount = CountEndpoints(solution, module);

            table.AddRow(
                $"[green]{Markup.Escape(module)}[/]",
                Markup.Escape(routePrefix ?? "—"),
                endpointCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine(
            $"\n[dim]{solution.ExistingModules.Count} module(s) in {Markup.Escape(solution.RootPath)}[/]"
        );
        return 0;
    }

    private static string? ReadRoutePrefix(SolutionContext solution, string module)
    {
        var moduleClassPath = Path.Combine(
            solution.GetModuleProjectPath(module),
            $"{module}Module.cs"
        );
        if (!File.Exists(moduleClassPath))
        {
            return null;
        }

        var content = File.ReadAllText(moduleClassPath);
        var match = RoutePrefixRegex().Match(content);
        if (!match.Success)
        {
            return null;
        }

        if (match.Groups["literal"].Success)
        {
            return match.Groups["literal"].Value;
        }

        // RoutePrefix = ModuleConstants.RoutePrefix — try to resolve from Constants.cs
        var constantsPath = Path.Combine(
            solution.GetModuleProjectPath(module),
            $"{module}Constants.cs"
        );
        if (File.Exists(constantsPath))
        {
            var constantsMatch = ConstantsRoutePrefixRegex().Match(File.ReadAllText(constantsPath));
            if (constantsMatch.Success)
            {
                return constantsMatch.Groups["value"].Value;
            }
        }

        return match.Groups["constref"].Value;
    }

    private static int CountEndpoints(SolutionContext solution, string module)
    {
        var endpointsDir = Path.Combine(solution.GetModuleProjectPath(module), "Endpoints");
        if (!Directory.Exists(endpointsDir))
        {
            return 0;
        }

        return Directory
            .EnumerateFiles(endpointsDir, "*Endpoint.cs", SearchOption.AllDirectories)
            .Count();
    }
}
