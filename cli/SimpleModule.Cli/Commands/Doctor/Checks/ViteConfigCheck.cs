using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class ViteConfigCheck : IDoctorCheck
{
    private static readonly string[] RequiredExternals = ["react", "react-dom", "@inertiajs/react"];

    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var viteConfigPath = Path.Combine(
                solution.GetModuleProjectPath(module),
                "vite.config.ts"
            );

            if (!File.Exists(viteConfigPath))
            {
                yield return new CheckResult(
                    $"{module} vite.config.ts",
                    CheckStatus.Warning,
                    "vite.config.ts not found — module won't build as a library"
                );
                continue;
            }

            var content = File.ReadAllText(viteConfigPath);
            var hasLibMode = content.Contains("lib:", StringComparison.Ordinal);
            var missingExternals = RequiredExternals
                .Where(e => !content.Contains(e, StringComparison.Ordinal))
                .ToList();

            if (hasLibMode && missingExternals.Count == 0)
            {
                yield return new CheckResult(
                    $"{module} vite.config.ts",
                    CheckStatus.Pass,
                    "library mode configured with correct externals"
                );
            }
            else
            {
                var issues = new List<string>();
                if (!hasLibMode)
                    issues.Add("missing lib mode");
                if (missingExternals.Count > 0)
                    issues.Add($"missing externals: {string.Join(", ", missingExternals)}");
                yield return new CheckResult(
                    $"{module} vite.config.ts",
                    CheckStatus.Warning,
                    string.Join("; ", issues)
                );
            }
        }
    }
}
