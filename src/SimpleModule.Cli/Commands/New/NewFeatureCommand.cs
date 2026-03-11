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
            AnsiConsole.MarkupLine("[red]Could not find .slnx file. Run this command from within a SimpleModule project.[/]");
            return 1;
        }

        if (solution.ExistingModules.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No modules found. Create a module first with 'sm new module'.[/]");
            return 1;
        }

        var moduleName = settings.ResolveModule(solution);
        var featureName = settings.ResolveName();
        var httpMethod = settings.ResolveHttpMethod();
        var route = settings.ResolveRoute();
        var includeValidator = settings.ResolveIncludeValidator();
        var singularName = ModuleTemplates.GetSingularName(moduleName);

        var templates = new FeatureTemplates(solution);

        var featureDir = Path.Combine(solution.GetModuleProjectPath(moduleName), "Features", featureName);
        Directory.CreateDirectory(featureDir);

        // Create endpoint
        var endpointPath = Path.Combine(featureDir, $"{featureName}Endpoint.cs");
        File.WriteAllText(endpointPath, templates.Endpoint(moduleName, featureName, httpMethod, route, singularName));
        AnsiConsole.MarkupLine($"[green]  + {featureName}Endpoint.cs[/]");

        // Create validator if requested
        if (includeValidator)
        {
            var validatorPath = Path.Combine(featureDir, $"{featureName}RequestValidator.cs");
            File.WriteAllText(validatorPath, templates.Validator(moduleName, featureName, singularName));
            AnsiConsole.MarkupLine($"[green]  + {featureName}RequestValidator.cs[/]");
        }

        // Wire into module class
        var moduleFilePath = Path.Combine(solution.GetModuleProjectPath(moduleName), $"{moduleName}Module.cs");
        if (ModuleClassManipulator.AddFeatureWiring(moduleFilePath, moduleName, featureName))
        {
            AnsiConsole.MarkupLine($"[green]  + Wired {featureName}Endpoint into {moduleName}Module.cs[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]  ! Could not auto-wire into {moduleName}Module.cs. Add manually:[/]");
            AnsiConsole.MarkupLine($"[yellow]    using SimpleModule.{moduleName}.Features.{featureName};[/]");
            AnsiConsole.MarkupLine($"[yellow]    {featureName}Endpoint.Map(group);[/]");
        }

        AnsiConsole.MarkupLine($"[green]Feature '{featureName}' added to '{moduleName}'.[/]");
        return 0;
    }
}
