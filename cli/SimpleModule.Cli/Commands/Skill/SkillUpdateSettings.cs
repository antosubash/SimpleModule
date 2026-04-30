using System.ComponentModel;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Skill;

public sealed class SkillUpdateSettings : CommandSettings
{
    [CommandArgument(0, "[name]")]
    [Description("Skill name to update. Omit to update every skill in skills-lock.json.")]
    public string? Name { get; set; }

    [CommandOption("--ref <REF>")]
    [Description(
        "Override the GitHub ref (branch, tag, or SHA) used when re-fetching. Applies only to the named skill."
    )]
    public string? Ref { get; set; }

    [CommandOption("--check")]
    [Description("Report drift between recorded hashes and remote sources without writing files.")]
    public bool Check { get; set; }

    [CommandOption("--dry-run")]
    [Description("Show what would change without modifying any files.")]
    public bool DryRun { get; set; }
}
