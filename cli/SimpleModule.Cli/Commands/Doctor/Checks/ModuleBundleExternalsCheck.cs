using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

/// <summary>
/// Detects bundles that inline host-provided libraries (a duplicate React copy
/// breaks hooks at runtime). Same validation `sm pack` enforces, surfaced
/// during development.
/// </summary>
public sealed class ModuleBundleExternalsCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(Infrastructure.SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var wwwroot = Path.Combine(solution.GetModuleProjectPath(module), "wwwroot");
            if (!Directory.Exists(wwwroot))
            {
                continue;
            }

            var violations = BundleExternalsValidator.Validate(wwwroot);
            if (violations.Count == 0)
            {
                yield return new CheckResult(
                    $"{module} bundle externals",
                    CheckStatus.Pass,
                    "no inlined React markers"
                );
            }
            else
            {
                foreach (var violation in violations)
                {
                    yield return new CheckResult(
                        $"{module} bundle externals",
                        CheckStatus.Fail,
                        $"{Path.GetFileName(violation.File)} contains inlined-React marker {violation.Marker} — "
                            + "externalize react/react-dom/@inertiajs/react via defineModuleConfig"
                    );
                }
            }
        }
    }
}
