using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.New;

public sealed class NewAgentSettings : CommandSettings
{
    [CommandArgument(0, "[name]")]
    [Description("Agent name in PascalCase (e.g. CustomerSupport)")]
    public string? Name { get; set; }

    [CommandOption("--module <MODULE>")]
    [Description("Target module name (e.g. Products)")]
    public string? Module { get; set; }

    [CommandOption("--dry-run")]
    [Description("Show what would be created without writing any files")]
    public bool DryRun { get; set; }

    public string ResolveName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Name = AnsiConsole.Ask<string>("Agent name (PascalCase):");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Agent name cannot be empty.");
        }

        if (!char.IsUpper(Name[0]))
        {
            throw new InvalidOperationException(
                $"'{Name}' is not PascalCase. Did you mean '{char.ToUpperInvariant(Name[0])}{Name[1..]}'?"
            );
        }

        return Name;
    }

    public string ResolveModule(IReadOnlyList<string> existingModules)
    {
        if (string.IsNullOrWhiteSpace(Module))
        {
            Module = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which module should the agent be added to?")
                    .AddChoices(existingModules)
            );
        }

        return Module;
    }
}
