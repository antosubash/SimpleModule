using System.ComponentModel;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.New;

public sealed class NewFeatureSettings : CommandSettings
{
    [CommandArgument(0, "[name]")]
    [Description("Feature name in PascalCase (e.g. UpdateInvoice)")]
    public string? Name { get; set; }

    [CommandOption("-m|--module")]
    [Description("Target module name")]
    public string? Module { get; set; }

    [CommandOption("--method")]
    [Description("HTTP method (GET, POST, PUT, DELETE)")]
    public string? HttpMethod { get; set; }

    [CommandOption("-r|--route")]
    [Description("Route pattern (e.g. /{id})")]
    public string? Route { get; set; }

    [CommandOption("--validator")]
    [Description("Include a validator class")]
    public bool IncludeValidator { get; set; }

    public string ResolveName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Name = AnsiConsole.Ask<string>("Feature name (PascalCase):");
        }

        return Name;
    }

    public string ResolveModule(SolutionContext solution)
    {
        if (string.IsNullOrWhiteSpace(Module))
        {
            Module = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a module:")
                    .AddChoices(solution.ExistingModules));
        }

        return Module;
    }

    public string ResolveHttpMethod()
    {
        if (string.IsNullOrWhiteSpace(HttpMethod))
        {
            HttpMethod = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("HTTP method:")
                    .AddChoices("GET", "POST", "PUT", "DELETE"));
        }

        return HttpMethod.ToUpperInvariant();
    }

    public string ResolveRoute()
    {
        if (string.IsNullOrWhiteSpace(Route))
        {
            Route = AnsiConsole.Ask("Route pattern:", "/{id}");
        }

        return Route;
    }

    public bool ResolveIncludeValidator()
    {
        if (!IncludeValidator)
        {
            IncludeValidator = AnsiConsole.Confirm("Include a validator?", defaultValue: false);
        }

        return IncludeValidator;
    }
}
