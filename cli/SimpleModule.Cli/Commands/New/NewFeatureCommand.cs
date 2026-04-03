using SimpleModule.Cli.Infrastructure;
using SimpleModule.Cli.Templates;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.New;

public sealed class NewFeatureCommand : Command<NewFeatureSettings>
{
    public override int Execute(CommandContext context, NewFeatureSettings settings)
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
                "[red]No modules found. Create a module first with 'sm new module'.[/]"
            );
            return 1;
        }

        var moduleName = settings.ResolveModule(solution);
        var featureName = settings.ResolveName();
        var httpMethod = settings.ResolveHttpMethod();
        var route = settings.ResolveRoute();
        var includeValidator = settings.ResolveIncludeValidator();
        var singularName = ModuleTemplates.GetSingularName(moduleName);

        var templates = new FeatureTemplates(solution);
        var ops = new List<(string Path, FileAction Action)>();

        var endpointsDir = Path.Combine(
            solution.GetModuleProjectPath(moduleName),
            "Endpoints",
            moduleName
        );

        ops.Add((Path.Combine(endpointsDir, $"{featureName}Endpoint.cs"), FileAction.Create));
        if (includeValidator)
            ops.Add(
                (Path.Combine(endpointsDir, $"{featureName}RequestValidator.cs"), FileAction.Create)
            );

        if (!settings.NoView)
        {
            ops.Add(
                (
                    Path.Combine(solution.GetModuleViewsPath(moduleName), $"{featureName}.tsx"),
                    FileAction.Create
                )
            );
            ops.Add((solution.GetModulePagesIndexPath(moduleName), FileAction.Modify));
        }

        if (settings.DryRun)
        {
            RenderDryRunTree(ops);
            return 0;
        }

        Directory.CreateDirectory(endpointsDir);
        WriteFile(
            Path.Combine(endpointsDir, $"{featureName}Endpoint.cs"),
            templates.Endpoint(moduleName, featureName, httpMethod, route, singularName)
        );

        if (includeValidator)
            WriteFile(
                Path.Combine(endpointsDir, $"{featureName}RequestValidator.cs"),
                templates.Validator(moduleName, featureName, singularName)
            );

        if (!settings.NoView)
        {
            var viewsDir = solution.GetModuleViewsPath(moduleName);
            Directory.CreateDirectory(viewsDir);
            WriteFile(
                Path.Combine(viewsDir, $"{featureName}.tsx"),
                FeatureTemplates.ViewComponent(moduleName, featureName)
            );

            var indexPath = solution.GetModulePagesIndexPath(moduleName);
            PagesRegistryFixer.AddEntry(
                indexPath,
                $"{moduleName}/{featureName}",
                $"@/Views/{featureName}"
            );
            AnsiConsole.MarkupLine($"[green]  ~ Pages/index.ts[/]");
        }

        AnsiConsole.MarkupLine($"\n[green]Feature '{featureName}' added to '{moduleName}'.[/]");
        return 0;
    }

    private static void WriteFile(string path, string content)
    {
        File.WriteAllText(path, content);
        AnsiConsole.MarkupLine($"[green]  + {Markup.Escape(Path.GetFileName(path))}[/]");
    }

    private static void RenderDryRunTree(List<(string Path, FileAction Action)> ops)
    {
        AnsiConsole.MarkupLine("[dim]Dry run — no files written[/]\n");
        var tree = new Tree("[dim]Would create/modify:[/]");
        foreach (var (path, action) in ops)
        {
            var label =
                action == FileAction.Modify
                    ? $"[yellow]{Markup.Escape(Path.GetFileName(path))}[/] [dim](modify)[/]"
                    : $"[green]{Markup.Escape(Path.GetFileName(path))}[/] [dim](create)[/]";
            tree.AddNode(label);
        }
        AnsiConsole.Write(tree);
    }
}
