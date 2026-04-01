using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class PackageJsonCheck : IDoctorCheck
{
    private static readonly string[] RequiredPeerDeps = ["react", "@inertiajs/react"];

    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var packageJsonPath = Path.Combine(
                solution.GetModuleProjectPath(module),
                "package.json"
            );

            if (!File.Exists(packageJsonPath))
            {
                yield return new CheckResult(
                    $"{module} package.json",
                    CheckStatus.Warning,
                    "package.json not found"
                );
                continue;
            }

            var content = File.ReadAllText(packageJsonPath);
            var peerDepsStart = content.IndexOf("\"peerDependencies\"", StringComparison.Ordinal);

            var missingFromPeerDeps = RequiredPeerDeps
                .Where(dep =>
                {
                    if (peerDepsStart < 0)
                        return true;
                    var searchFrom = peerDepsStart;
                    var idx = content.IndexOf($"\"{dep}\"", searchFrom, StringComparison.Ordinal);
                    return idx < 0;
                })
                .ToList();

            if (missingFromPeerDeps.Count == 0)
            {
                yield return new CheckResult(
                    $"{module} package.json",
                    CheckStatus.Pass,
                    "React and @inertiajs/react declared as peerDependencies"
                );
            }
            else
            {
                yield return new CheckResult(
                    $"{module} package.json",
                    CheckStatus.Warning,
                    $"missing from peerDependencies: {string.Join(", ", missingFromPeerDeps)}"
                );
            }
        }
    }
}
