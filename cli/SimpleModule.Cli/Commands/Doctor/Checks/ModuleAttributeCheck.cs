using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class ModuleAttributeCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var moduleClassPath = Path.Combine(
                solution.GetModuleProjectPath(module),
                $"{module}Module.cs"
            );

            if (!File.Exists(moduleClassPath))
            {
                yield return new CheckResult(
                    $"{module} [Module] attribute",
                    CheckStatus.Warning,
                    $"{module}Module.cs not found — skipping attribute check"
                );
                continue;
            }

            var content = File.ReadAllText(moduleClassPath);
            var hasAttribute = content.Contains("[Module(", StringComparison.Ordinal);
            var hasRoutePrefix = content.Contains("RoutePrefix", StringComparison.Ordinal);

            yield return (hasAttribute && hasRoutePrefix)
                ? new CheckResult(
                    $"{module} [Module] attribute",
                    CheckStatus.Pass,
                    "[Module] attribute with RoutePrefix present"
                )
                : new CheckResult(
                    $"{module} [Module] attribute",
                    CheckStatus.Fail,
                    hasAttribute
                        ? "[Module] attribute present but RoutePrefix is missing"
                        : $"{module}Module.cs missing [Module] attribute"
                );
        }
    }
}
