namespace SimpleModule.Cli.Infrastructure;

public static class ModuleClassManipulator
{
    public static bool AddFeatureWiring(
        string moduleFilePath,
        string moduleName,
        string featureName
    )
    {
        if (!File.Exists(moduleFilePath))
        {
            return false;
        }

        var lines = File.ReadAllLines(moduleFilePath).ToList();

        // Add using directive
        var usingNamespace = $"using SimpleModule.{moduleName}.Features.{featureName};";
        var lastUsingIndex = lines.FindLastIndex(l =>
            l.TrimStart().StartsWith("using ", StringComparison.Ordinal)
        );

        if (lastUsingIndex >= 0 && !lines.Any(l => l.Trim() == usingNamespace))
        {
            lines.Insert(lastUsingIndex + 1, usingNamespace);
        }

        // Add endpoint mapping
        var mapCall = $"        {featureName}Endpoint.Map(group);";
        var lastMapIndex = lines.FindLastIndex(l =>
            l.Contains(".Map(group);", StringComparison.Ordinal)
        );

        if (
            lastMapIndex >= 0
            && !lines.Any(l =>
                l.Contains($"{featureName}Endpoint.Map(group);", StringComparison.Ordinal)
            )
        )
        {
            lines.Insert(lastMapIndex + 1, mapCall);
        }
        else if (lastMapIndex < 0)
        {
            return false; // Pattern not found
        }

        File.WriteAllLines(moduleFilePath, lines);
        return true;
    }
}
