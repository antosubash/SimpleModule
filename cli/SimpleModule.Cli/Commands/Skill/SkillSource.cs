using System.Text.RegularExpressions;

namespace SimpleModule.Cli.Commands.Skill;

public enum SkillSourceType
{
    GitHub,
    Local,
    Scaffold,
}

public sealed partial record SkillSource(
    SkillSourceType Type,
    string Raw,
    string? Owner = null,
    string? Repo = null,
    string? Path = null,
    string? Ref = null,
    string? LocalPath = null
)
{
    [GeneratedRegex(
        @"^(?<owner>[A-Za-z0-9][A-Za-z0-9-_.]*)/(?<repo>[A-Za-z0-9][A-Za-z0-9-_.]*)(?:/(?<path>[^@\s]+?))?(?:@(?<ref>[^\s]+))?$",
        RegexOptions.Singleline,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex GitHubSourceRegex();

    public string SourceTypeId =>
        Type switch
        {
            SkillSourceType.GitHub => "github",
            SkillSourceType.Local => "local",
            SkillSourceType.Scaffold => "scaffold",
            _ => "unknown",
        };

    public string CanonicalSource =>
        Type switch
        {
            SkillSourceType.GitHub => Path is null ? $"{Owner}/{Repo}" : $"{Owner}/{Repo}/{Path}",
            SkillSourceType.Local => LocalPath ?? Raw,
            SkillSourceType.Scaffold => "scaffold",
            _ => Raw,
        };

    public static SkillSource Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ArgumentException("Source cannot be empty.", nameof(raw));
        }

        // Local path (absolute, relative-with-dot, or contains a directory separator that isn't owner/repo)
        if (
            System.IO.Path.IsPathRooted(raw)
            || raw.StartsWith("./", StringComparison.Ordinal)
            || raw.StartsWith("../", StringComparison.Ordinal)
            || raw.StartsWith(".\\", StringComparison.Ordinal)
            || raw.StartsWith("..\\", StringComparison.Ordinal)
        )
        {
            return new SkillSource(SkillSourceType.Local, raw, LocalPath: raw);
        }

        var match = GitHubSourceRegex().Match(raw);
        if (match.Success)
        {
            return new SkillSource(
                SkillSourceType.GitHub,
                raw,
                Owner: match.Groups["owner"].Value,
                Repo: match.Groups["repo"].Value,
                Path: match.Groups["path"].Success ? match.Groups["path"].Value.TrimEnd('/') : null,
                Ref: match.Groups["ref"].Success ? match.Groups["ref"].Value : null
            );
        }

        // Fall back to treating as a local path
        return new SkillSource(SkillSourceType.Local, raw, LocalPath: raw);
    }
}
