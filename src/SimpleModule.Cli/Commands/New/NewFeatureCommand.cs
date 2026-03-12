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
                "[red]Could not find .slnx file. Run this command from within a SimpleModule project.[/]"
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

        var endpointsDir = Path.Combine(
            solution.GetModuleProjectPath(moduleName),
            "Endpoints",
            moduleName
        );
        Directory.CreateDirectory(endpointsDir);

        // Create endpoint (implements IEndpoint — auto-discovered by source generator)
        var endpointPath = Path.Combine(endpointsDir, $"{featureName}Endpoint.cs");
        File.WriteAllText(
            endpointPath,
            templates.Endpoint(moduleName, featureName, httpMethod, route, singularName)
        );
        AnsiConsole.MarkupLine($"[green]  + {featureName}Endpoint.cs[/]");

        // Create validator if requested
        if (includeValidator)
        {
            var validatorPath = Path.Combine(endpointsDir, $"{featureName}RequestValidator.cs");
            File.WriteAllText(
                validatorPath,
                templates.Validator(moduleName, featureName, singularName)
            );
            AnsiConsole.MarkupLine($"[green]  + {featureName}RequestValidator.cs[/]");
        }

        AnsiConsole.MarkupLine(
            $"[green]Endpoint '{featureName}' added to '{moduleName}' (auto-discovered via IEndpoint).[/]"
        );
        return 0;
    }
}
