using System.Text.RegularExpressions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed partial class NpmWorkspaceCheck : IDoctorCheck
{
    [GeneratedRegex(@"""workspaces""\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline)]
    private static partial Regex WorkspacesPattern();

    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        var rootPackageJson = Path.Combine(solution.RootPath, "package.json");

        if (!File.Exists(rootPackageJson))
        {
            foreach (var module in solution.ExistingModules)
                yield return new CheckResult($"NpmWorkspace -> {module}", CheckStatus.Warning,
                    "root package.json not found — cannot verify workspaces");
            yield break;
        }

        var content = File.ReadAllText(rootPackageJson);
        var match = WorkspacesPattern().Match(content);
        var workspaceGlobs = match.Success
            ? match.Groups[1].Value
                .Split(',')
                .Select(g => g.Trim().Trim('"'))
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .ToList()
            : [];

        foreach (var module in solution.ExistingModules)
        {
            var modulePath = $"src/modules/{module}/src/{module}";
            var isCovered = workspaceGlobs.Any(glob => GlobCoversPath(glob, modulePath));

            yield return isCovered
                ? new CheckResult($"NpmWorkspace -> {module}", CheckStatus.Pass, "covered by workspace glob")
                : new CheckResult($"NpmWorkspace -> {module}", CheckStatus.Fail,
                    $"'{modulePath}' not covered by any workspace glob in root package.json");
        }
    }

    private static bool GlobCoversPath(string glob, string path)
    {
        var pattern = "^" + Regex.Escape(glob).Replace(@"\*", "[^/]+", StringComparison.Ordinal) + "$";
        return Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase);
    }
}
