using System.Text.RegularExpressions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed partial class PagesRegistryCheck : IDoctorCheck
{
    [GeneratedRegex(@"Inertia\.Render\s*\(\s*""([^""]+)""")]
    private static partial Regex InertiaRenderPattern();

    [GeneratedRegex(@"""([^""]+)""\s*:\s*(?:\(\s*\)|(?:async\s*)?\(\s*\)\s*=>|import)")]
    private static partial Regex PagesKeyPattern();

    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var moduleDir = solution.GetModuleProjectPath(module);
            if (!Directory.Exists(moduleDir))
                continue;

            var csFiles = Directory.GetFiles(moduleDir, "*.cs", SearchOption.AllDirectories);
            var inertiaComponents = csFiles
                .SelectMany(f => InertiaRenderPattern().Matches(File.ReadAllText(f)))
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            if (inertiaComponents.Count == 0)
                continue;

            var indexPath = solution.GetModulePagesIndexPath(module);
            var registeredKeys = new HashSet<string>(StringComparer.Ordinal);

            if (File.Exists(indexPath))
            {
                var indexContent = File.ReadAllText(indexPath);
                foreach (Match m in PagesKeyPattern().Matches(indexContent))
                    registeredKeys.Add(m.Groups[1].Value);
            }

            foreach (var component in inertiaComponents)
            {
                yield return registeredKeys.Contains(component)
                    ? new CheckResult($"Pages -> {component}", CheckStatus.Pass, "registered in Pages/index.ts")
                    : new CheckResult($"Pages -> {component}", CheckStatus.Fail,
                        $"'{component}' used in Inertia.Render but missing from Pages/index.ts");
            }
        }
    }
}
