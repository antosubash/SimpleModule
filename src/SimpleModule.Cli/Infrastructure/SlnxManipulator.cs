namespace SimpleModule.Cli.Infrastructure;

public static class SlnxManipulator
{
    public static void AddModuleEntries(string slnxPath, string moduleName)
    {
        var content = File.ReadAllText(slnxPath);

        var folderName = $"/modules/{moduleName}/";
        if (content.Contains(folderName, StringComparison.OrdinalIgnoreCase))
        {
            return; // Already exists
        }

        // Detect indentation from existing file
        var lines = File.ReadAllLines(slnxPath).ToList();
        var indent = DetectIndent(lines);
        var indent2 = indent + indent;

        // Insert module folder before the /tests/ folder
        var moduleFolderLines = new[]
        {
            $"{indent}<Folder Name=\"/modules/{moduleName}/\">",
            $"{indent2}<Project Path=\"src/modules/{moduleName}/{moduleName}.Contracts/{moduleName}.Contracts.csproj\" />",
            $"{indent2}<Project Path=\"src/modules/{moduleName}/{moduleName}/{moduleName}.csproj\" />",
            $"{indent}</Folder>",
        };

        var testsIndex = lines.FindIndex(l =>
            l.Contains("<Folder Name=\"/tests/\">", StringComparison.Ordinal)
        );
        if (testsIndex >= 0)
        {
            lines.InsertRange(testsIndex, moduleFolderLines);
        }
        else
        {
            // Insert before </Solution>
            var endIndex = lines.FindIndex(l =>
                l.Contains("</Solution>", StringComparison.Ordinal)
            );
            if (endIndex >= 0)
            {
                lines.InsertRange(endIndex, moduleFolderLines);
            }
        }

        // Add test project to /tests/modules/ folder
        var testsModulesEndIndex = FindFolderClosingTag(lines, "/tests/modules/");
        if (testsModulesEndIndex >= 0)
        {
            var testProjectLine =
                $"{indent2}<Project Path=\"tests/modules/{moduleName}.Tests/{moduleName}.Tests.csproj\" />";
            lines.Insert(testsModulesEndIndex, testProjectLine);
        }

        File.WriteAllLines(slnxPath, lines);
    }

    public static bool HasModuleEntry(string slnxPath, string moduleName)
    {
        var content = File.ReadAllText(slnxPath);
        var folderName = $"/modules/{moduleName}/";
        return content.Contains(folderName, StringComparison.OrdinalIgnoreCase);
    }

    private static string DetectIndent(List<string> lines)
    {
        foreach (var line in lines)
        {
            if (line.Length > 0 && line[0] == ' ')
            {
                var spaces = line.Length - line.TrimStart().Length;
                return new string(' ', spaces);
            }

            if (line.Length > 0 && line[0] == '\t')
            {
                return "\t";
            }
        }

        return "    ";
    }

    private static int FindFolderClosingTag(List<string> lines, string folderName)
    {
        var folderIndex = lines.FindIndex(l =>
            l.Contains($"<Folder Name=\"{folderName}\">", StringComparison.Ordinal)
        );
        if (folderIndex < 0)
        {
            return -1;
        }

        // Find closing </Folder> tag
        for (var i = folderIndex + 1; i < lines.Count; i++)
        {
            if (lines[i].TrimStart().StartsWith("</Folder>", StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
