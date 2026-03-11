using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.New;

public sealed class NewModuleSettings : CommandSettings
{
    [CommandArgument(0, "[name]")]
    [Description("Module name in PascalCase (e.g. Invoices)")]
    public string? Name { get; set; }

    public string ResolveName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Name = AnsiConsole.Ask<string>("Module name (PascalCase):");
        }

        if (!char.IsUpper(Name[0]))
        {
            throw new InvalidOperationException("Module name must be PascalCase (start with uppercase letter).");
        }

        return Name;
    }
}
