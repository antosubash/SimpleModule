using System.Text.RegularExpressions;

namespace SimpleModule.Cli.Commands.Skill;

public static partial class SkillNameValidator
{
    [GeneratedRegex(
        "^[a-z0-9][a-z0-9-]{0,63}$",
        RegexOptions.Singleline,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex ValidNameRegex();

    public static bool IsValid(string name) =>
        !string.IsNullOrWhiteSpace(name) && ValidNameRegex().IsMatch(name);

    public static string Normalize(string name) => name.Trim().ToLowerInvariant();

    public static string ValidationMessage(string name) =>
        $"Skill name '{name}' is invalid. Use lowercase letters, digits, and hyphens (max 64 chars), starting with a letter or digit.";
}
