namespace SimpleModule.Cli.Infrastructure;

public static class ProjectManipulator
{
    public static void AddProjectReference(string csprojPath, string referencePath)
    {
        var content = File.ReadAllText(csprojPath);

        // Check if already referenced
        if (content.Contains(referencePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var lines = File.ReadAllLines(csprojPath).ToList();

        // Find last ProjectReference line
        var lastProjRefIndex = -1;
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            if (lines[i].Contains("<ProjectReference", StringComparison.Ordinal))
            {
                // Handle multi-line ProjectReference — find the closing />
                if (!lines[i].TrimEnd().EndsWith("/>", StringComparison.Ordinal))
                {
                    for (var j = i + 1; j < lines.Count; j++)
                    {
                        if (lines[j].TrimEnd().EndsWith("/>", StringComparison.Ordinal))
                        {
                            lastProjRefIndex = j;
                            break;
                        }
                    }
                }
                else
                {
                    lastProjRefIndex = i;
                }

                break;
            }
        }

        if (lastProjRefIndex >= 0)
        {
            // Detect indentation from the last ProjectReference line
            var refLine = lines[lastProjRefIndex];
            var indent = refLine[..^refLine.TrimStart().Length];

            // For multi-line refs, get indent from the opening <ProjectReference line
            for (var i = lastProjRefIndex; i >= 0; i--)
            {
                if (lines[i].Contains("<ProjectReference", StringComparison.Ordinal))
                {
                    indent = lines[i][..^lines[i].TrimStart().Length];
                    break;
                }
            }

            var newLine = $"{indent}<ProjectReference Include=\"{referencePath}\" />";
            lines.Insert(lastProjRefIndex + 1, newLine);
        }
        else
        {
            // No ProjectReference found, add ItemGroup before </Project>
            var endIndex = lines.FindIndex(l => l.TrimStart().StartsWith("</Project>", StringComparison.Ordinal));
            if (endIndex >= 0)
            {
                lines.InsertRange(endIndex,
                [
                    "  <ItemGroup>",
                    $"    <ProjectReference Include=\"{referencePath}\" />",
                    "  </ItemGroup>",
                ]);
            }
        }

        File.WriteAllLines(csprojPath, lines);
    }

    public static bool HasProjectReference(string csprojPath, string moduleName)
    {
        if (!File.Exists(csprojPath))
        {
            return false;
        }

        var content = File.ReadAllText(csprojPath);
        return content.Contains(moduleName, StringComparison.OrdinalIgnoreCase)
               && content.Contains("<ProjectReference", StringComparison.Ordinal);
    }
}
