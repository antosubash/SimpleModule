using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.New;

public sealed class NewModuleSettings : CommandSettings
{
    [CommandArgument(0, "[name]")]
    [Description("Module name in PascalCase (e.g. Invoices)")]
    public string? Name { get; set; }

    [CommandOption("--dry-run")]
    [Description("Show what would be created without writing any files")]
    public bool DryRun { get; set; }

    public string ResolveName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Name = AnsiConsole.Ask<string>("Module name (PascalCase):");
        }

        if (!char.IsUpper(Name[0]))
        {
            throw new InvalidOperationException(
                $"'{Name}' is not PascalCase. Did you mean '{char.ToUpperInvariant(Name[0])}{Name[1..]}'?"
            );
        }

        return Name;
    }
}
