using System.ComponentModel;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Maintenance;

public sealed class DownSettings : CommandSettings
{
    [CommandOption("--secret <SECRET>")]
    [Description(
        "Shared secret. Visiting ?sm_bypass=<SECRET> sets a bypass cookie. Generated when omitted."
    )]
    public string? Secret { get; set; }

    [CommandOption("-m|--message <MESSAGE>")]
    [Description("Message shown on the maintenance page")]
    public string? Message { get; set; }

    [CommandOption("--retry <SECONDS>")]
    [Description("Value sent in the Retry-After response header. Defaults to 60")]
    public int? RetryAfterSeconds { get; set; }

    [CommandOption("--until <ISO8601>")]
    [Description("Optional ISO-8601 timestamp at which maintenance auto-clears (UTC if no offset)")]
    public string? Until { get; set; }

    [CommandOption("--status")]
    [Description("Print the current maintenance state without modifying it")]
    public bool Status { get; set; }
}
