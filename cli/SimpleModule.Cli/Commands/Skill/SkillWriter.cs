namespace SimpleModule.Cli.Commands.Skill;

public static class SkillWriter
{
    public static string GetSkillsRoot(string solutionRoot) =>
        Path.Combine(solutionRoot, ".claude", "skills");

    public static string GetSkillDirectory(string solutionRoot, string skillName) =>
        Path.Combine(GetSkillsRoot(solutionRoot), skillName);

    public static IReadOnlyList<string> WriteFiles(
        string skillDirectory,
        IReadOnlyList<FetchedSkillFile> files,
        bool replace
    )
    {
        if (replace && Directory.Exists(skillDirectory))
        {
            Directory.Delete(skillDirectory, recursive: true);
        }

        Directory.CreateDirectory(skillDirectory);

        var written = new List<string>();
        foreach (var file in files)
        {
            var relative = file.RelativePath.Replace('\\', '/').TrimStart('/');
            var fullPath = Path.GetFullPath(Path.Combine(skillDirectory, relative));
            var skillDirFull = Path.GetFullPath(skillDirectory);
            if (
                !fullPath.StartsWith(
                    skillDirFull + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal
                ) && !string.Equals(fullPath, skillDirFull, StringComparison.Ordinal)
            )
            {
                throw new InvalidOperationException(
                    $"Refusing to write outside skill directory: {fullPath}"
                );
            }

            var parent = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.WriteAllBytes(fullPath, file.GetContent());
            written.Add(relative);
        }

        return written;
    }

    public static FetchedSkill BuildScaffold(string skillName, string? description)
    {
        var trimmedDescription = string.IsNullOrWhiteSpace(description)
            ? $"Describe when Claude should use the '{skillName}' skill."
            : description!.Trim();

        var skillMd = $$"""
            ---
            name: {{skillName}}
            description: >
              {{trimmedDescription}}
            ---

            # {{skillName}}

            Replace this with guidance for the skill. Document when it should be used,
            the conventions to follow, and any references that elaborate on subtopics.
            """;

        var files = new List<FetchedSkillFile>
        {
            new("SKILL.md", System.Text.Encoding.UTF8.GetBytes(skillMd)),
        };
        return new FetchedSkill(files, SkillFetcher.ComputeHash(files));
    }
}
