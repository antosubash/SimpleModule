using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.New;

public sealed class NewProjectSettings : CommandSettings
{
    [CommandArgument(0, "[name]")]
    [Description("Project name in PascalCase (e.g. MyApp)")]
    public string? Name { get; set; }

    [CommandOption("-o|--output")]
    [Description("Output directory (defaults to current directory)")]
    public string? OutputDir { get; set; }

    [CommandOption("--dry-run")]
    [Description("Show what would be created without writing any files")]
    public bool DryRun { get; set; }

    public string ResolveName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Name = AnsiConsole.Ask<string>("Project name (PascalCase):");
        }

        return Name;
    }

    public string ResolveOutputDir()
    {
        if (string.IsNullOrWhiteSpace(OutputDir))
        {
            OutputDir = Directory.GetCurrentDirectory();
        }

        return OutputDir;
    }
}
