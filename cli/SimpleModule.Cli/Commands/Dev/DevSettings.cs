using System.ComponentModel;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Dev;

public sealed class DevSettings : CommandSettings
{
    [CommandOption("--no-vite")]
    [Description("Skip starting the Vite dev server (frontend only rebuilds via file watcher)")]
    public bool NoVite { get; set; }

    [CommandOption("--no-dotnet")]
    [Description("Skip starting the .NET backend (useful when running it separately)")]
    public bool NoDotnet { get; set; }

    [CommandOption("--vite-port")]
    [Description("Port for the Vite dev server (default: 5173)")]
    [DefaultValue(5173)]
    public int VitePort { get; set; } = 5173;
}
