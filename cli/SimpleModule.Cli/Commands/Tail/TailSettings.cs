using System.ComponentModel;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Tail;

public sealed class TailSettings : CommandSettings
{
    [CommandOption("-l|--level")]
    [Description(
        "Minimum log level to display (Trace, Debug, Information, Warning, Error, Critical). Short forms info/warn/err accepted."
    )]
    public string? Level { get; set; }

    [CommandOption("-f|--filter")]
    [Description("Substring match against the rendered message")]
    public string? Filter { get; set; }

    [CommandOption("-s|--source")]
    [Description("Logger category (equals or starts-with match)")]
    public string? Source { get; set; }

    [CommandOption("-u|--user")]
    [Description("Match against the UserId property")]
    public string? User { get; set; }

    [CommandOption("-r|--request")]
    [Description("Match against the RequestId property")]
    public string? Request { get; set; }

    [CommandOption("--json")]
    [Description("Pass through raw JSON lines without coloring; skip plain-text lines")]
    public bool Json { get; set; }

    [CommandOption("--no-follow")]
    [Description("Read once and exit (defaults to follow mode)")]
    public bool NoFollow { get; set; }

    [CommandOption("--file <PATH>")]
    [Description(
        "Read from a file instead of stdin. Repeatable; multiple files are tailed concurrently and prefixed with [filename]"
    )]
#pragma warning disable CA1819 // Spectre.Console.Cli binds repeatable options to arrays
    public string[]? Files { get; set; }
#pragma warning restore CA1819

    [CommandOption("--no-color")]
    [Description(
        "Disable colored output. Also auto-disabled when NO_COLOR is set or output is redirected"
    )]
    public bool NoColor { get; set; }
}
