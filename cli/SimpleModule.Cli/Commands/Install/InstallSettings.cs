using System.ComponentModel;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Install;

public sealed class InstallSettings : CommandSettings
{
    [CommandArgument(0, "<package-id>")]
    [Description("The NuGet package ID to install (e.g. SimpleModule.MyModule)")]
    public string PackageId { get; set; } = string.Empty;

    [CommandOption("--version <VERSION>")]
    [Description("Specific package version to install")]
    public string? Version { get; set; }
}
