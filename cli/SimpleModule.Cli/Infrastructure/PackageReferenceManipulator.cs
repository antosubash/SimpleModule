using System.Text.RegularExpressions;

namespace SimpleModule.Cli.Infrastructure;

public readonly record struct PackageReferenceEntry(string Id, string? Version);

/// <summary>
/// Adds/removes NuGet package references on a host project, transparently
/// handling Central Package Management: with CPM the version lives in
/// Directory.Packages.props and the csproj reference is version-less (NU1008);
/// without CPM the version is inlined on the reference.
/// All edits are line-based to preserve the user's file formatting.
/// </summary>
public static partial class PackageReferenceManipulator
{
    public static void AddPackage(
        string csprojPath,
        string solutionRoot,
        string packageId,
        string version
    )
    {
        var propsPath = CpmPropsPath(solutionRoot);
        if (propsPath is not null)
        {
            SetCpmPackageVersion(propsPath, packageId, version);
            InsertReferenceLine(csprojPath, $"<PackageReference Include=\"{packageId}\" />");
        }
        else
        {
            InsertReferenceLine(
                csprojPath,
                $"<PackageReference Include=\"{packageId}\" Version=\"{version}\" />"
            );
        }
    }

    public static bool RemovePackage(string csprojPath, string solutionRoot, string packageId)
    {
        var removed = RemoveElementLine(csprojPath, "PackageReference", packageId);

        var propsPath = CpmPropsPath(solutionRoot);
        if (propsPath is not null)
        {
            removed |= RemoveElementLine(propsPath, "PackageVersion", packageId);
        }

        return removed;
    }

    public static IReadOnlyList<PackageReferenceEntry> GetPackageReferences(
        string csprojPath,
        string solutionRoot
    )
    {
        if (!File.Exists(csprojPath))
        {
            return [];
        }

        var cpmVersions = ReadCpmVersions(solutionRoot);
        var results = new List<PackageReferenceEntry>();
        foreach (Match match in PackageReferenceRegex().Matches(File.ReadAllText(csprojPath)))
        {
            var id = match.Groups["id"].Value;
            var version = match.Groups["version"].Success
                ? match.Groups["version"].Value
                : cpmVersions.GetValueOrDefault(id);
            results.Add(new PackageReferenceEntry(id, version));
        }

        return results;
    }

    private static string? CpmPropsPath(string solutionRoot)
    {
        var path = Path.Combine(solutionRoot, "Directory.Packages.props");
        return File.Exists(path) ? path : null;
    }

    private static Dictionary<string, string> ReadCpmVersions(string solutionRoot)
    {
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var propsPath = CpmPropsPath(solutionRoot);
        if (propsPath is null)
        {
            return versions;
        }

        foreach (Match match in PackageVersionRegex().Matches(File.ReadAllText(propsPath)))
        {
            versions[match.Groups["id"].Value] = match.Groups["version"].Value;
        }

        return versions;
    }

    private static void SetCpmPackageVersion(string propsPath, string packageId, string version)
    {
        var lines = File.ReadAllLines(propsPath).ToList();
        var token = $"<PackageVersion Include=\"{packageId}\"";

        var existing = lines.FindIndex(l => l.Contains(token, StringComparison.Ordinal));
        if (existing >= 0)
        {
            var indent = lines[existing][..^lines[existing].TrimStart().Length];
            lines[existing] =
                $"{indent}<PackageVersion Include=\"{packageId}\" Version=\"{version}\" />";
            File.WriteAllLines(propsPath, lines);
            return;
        }

        var anchor = lines.FindLastIndex(l =>
            l.Contains("<PackageVersion ", StringComparison.Ordinal)
        );
        if (anchor < 0)
        {
            anchor = lines.FindIndex(l => l.Contains("<ItemGroup>", StringComparison.Ordinal));
            if (anchor < 0)
            {
                throw new InvalidOperationException(
                    $"Could not find an <ItemGroup> in {propsPath} to add the PackageVersion entry."
                );
            }
        }

        var indentation = DetectIndent(lines[anchor], fallback: "    ");
        lines.Insert(
            anchor + 1,
            $"{indentation}<PackageVersion Include=\"{packageId}\" Version=\"{version}\" />"
        );
        File.WriteAllLines(propsPath, lines);
    }

    private static void InsertReferenceLine(string csprojPath, string element)
    {
        var content = File.ReadAllText(csprojPath);
        var includeToken = ExtractIncludeToken(element);
        if (content.Contains(includeToken, StringComparison.Ordinal))
        {
            return;
        }

        var lines = File.ReadAllLines(csprojPath).ToList();
        var anchor = lines.FindLastIndex(l =>
            l.Contains("<PackageReference ", StringComparison.Ordinal)
        );
        if (anchor < 0)
        {
            anchor = lines.FindLastIndex(l =>
                l.Contains("<ProjectReference ", StringComparison.Ordinal)
            );
        }

        if (anchor >= 0)
        {
            lines.Insert(anchor + 1, DetectIndent(lines[anchor], "    ") + element);
        }
        else
        {
            var close = lines.FindIndex(l => l.Contains("</Project>", StringComparison.Ordinal));
            if (close < 0)
            {
                throw new InvalidOperationException($"{csprojPath} has no closing </Project> tag.");
            }

            lines.Insert(close, "  <ItemGroup>");
            lines.Insert(close + 1, "    " + element);
            lines.Insert(close + 2, "  </ItemGroup>");
        }

        File.WriteAllLines(csprojPath, lines);
    }

    private static bool RemoveElementLine(string filePath, string elementName, string packageId)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        var token = $"<{elementName} Include=\"{packageId}\"";
        var lines = File.ReadAllLines(filePath).ToList();
        var removedCount = lines.RemoveAll(l => l.Contains(token, StringComparison.Ordinal));
        if (removedCount > 0)
        {
            File.WriteAllLines(filePath, lines);
        }

        return removedCount > 0;
    }

    private static string DetectIndent(string line, string fallback)
    {
        var indent = line[..^line.TrimStart().Length];
        return indent.Length > 0 ? indent : fallback;
    }

    private static string ExtractIncludeToken(string element)
    {
        var match = IncludeRegex().Match(element);
        return match.Success ? match.Value : element;
    }

    [GeneratedRegex(
        "<PackageReference\\s+Include=\"(?<id>[^\"]+)\"(?:\\s+Version=\"(?<version>[^\"]+)\")?"
    )]
    private static partial Regex PackageReferenceRegex();

    [GeneratedRegex(
        "<PackageVersion\\s+Include=\"(?<id>[^\"]+)\"\\s+Version=\"(?<version>[^\"]+)\""
    )]
    private static partial Regex PackageVersionRegex();

    [GeneratedRegex("Include=\"[^\"]+\"")]
    private static partial Regex IncludeRegex();
}
