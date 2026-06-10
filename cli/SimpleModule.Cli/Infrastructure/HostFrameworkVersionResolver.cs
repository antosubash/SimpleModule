using System.Text.Json;
using System.Text.RegularExpressions;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Determines the SimpleModule framework version a host application targets:
/// the CPM-pinned SimpleModule.Core PackageVersion, else version.json (the
/// in-repo development convention), else unknown.
/// </summary>
public static partial class HostFrameworkVersionResolver
{
    public static string? Resolve(string solutionRoot)
    {
        var propsPath = Path.Combine(solutionRoot, "Directory.Packages.props");
        if (File.Exists(propsPath))
        {
            var match = CorePackageVersionRegex().Match(File.ReadAllText(propsPath));
            if (match.Success)
            {
                return match.Groups["version"].Value;
            }
        }

        var versionJsonPath = Path.Combine(solutionRoot, "version.json");
        if (File.Exists(versionJsonPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(versionJsonPath));
                if (doc.RootElement.TryGetProperty("version", out var version))
                {
                    return version.GetString();
                }
            }
            catch (JsonException)
            {
                // fall through to null
            }
        }

        return null;
    }

    [GeneratedRegex(
        "<PackageVersion\\s+Include=\"SimpleModule\\.Core\"\\s+Version=\"(?<version>[^\"]+)\""
    )]
    private static partial Regex CorePackageVersionRegex();
}
