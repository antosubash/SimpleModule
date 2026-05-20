using System.ComponentModel;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Maintenance;

public sealed class DownSettings : CommandSettings
{
    [CommandOption("--secret <SECRET>")]
    [Description("Bypass secret. Visitors with ?sm_bypass=<secret> are allowed through.")]
    public string? Secret { get; set; }

    [CommandOption("--message <MESSAGE>")]
    [Description("Human-readable message shown on the 503 page.")]
    public string? Message { get; set; }

    [CommandOption("--retry <SECONDS>")]
    [Description("Value of the Retry-After header. Defaults to 60.")]
    [DefaultValue(60)]
    public int RetryAfterSeconds { get; set; } = 60;

    [CommandOption("--until <ISO_TIMESTAMP>")]
    [Description(
        "Optional ISO-8601 timestamp shown to operators (informational; the sentinel still requires `sm up`)."
    )]
    public string? Until { get; set; }

    [CommandOption("--status")]
    [Description("Print the current maintenance state and exit.")]
    public bool Status { get; set; }
}
