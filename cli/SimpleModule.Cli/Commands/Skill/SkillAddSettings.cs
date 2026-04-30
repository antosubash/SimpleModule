using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Skill;

public sealed class SkillAddSettings : CommandSettings
{
    [CommandArgument(0, "[name]")]
    [Description("Skill name in kebab-case (e.g. shadcn, react-expert)")]
    public string? Name { get; set; }

    [CommandOption("--source <SOURCE>")]
    [Description(
        "Source for the skill. GitHub: 'owner/repo' optionally followed by '/path' and '@ref'. Local: a directory path. Omit to scaffold a new template."
    )]
    public string? Source { get; set; }

    [CommandOption("--description <TEXT>")]
    [Description("Description used when scaffolding a new skill (only when --source is omitted).")]
    public string? Description { get; set; }

    [CommandOption("--force")]
    [Description("Overwrite the skill directory if it already exists.")]
    public bool Force { get; set; }

    [CommandOption("--dry-run")]
    [Description("Show what would be written without modifying any files.")]
    public bool DryRun { get; set; }

    public string ResolveName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Name = AnsiConsole.Ask<string>("Skill name (kebab-case):");
        }

        Name = SkillNameValidator.Normalize(Name);
        if (!SkillNameValidator.IsValid(Name))
        {
            throw new InvalidOperationException(SkillNameValidator.ValidationMessage(Name));
        }

        return Name;
    }
}
