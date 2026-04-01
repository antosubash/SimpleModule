using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class ViewEndpointNamingCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var endpointsDir = Path.Combine(
                solution.GetModuleProjectPath(module),
                "Endpoints",
                module
            );
            if (!Directory.Exists(endpointsDir))
                continue;

            foreach (var file in Directory.GetFiles(endpointsDir, "*.cs"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.EndsWith("Validator", StringComparison.Ordinal))
                    continue;

                yield return fileName.EndsWith("Endpoint", StringComparison.Ordinal)
                    ? new CheckResult(
                        $"{module}/{fileName}",
                        CheckStatus.Pass,
                        "follows Endpoint naming convention"
                    )
                    : new CheckResult(
                        $"{module}/{fileName}",
                        CheckStatus.Warning,
                        $"'{fileName}' does not end with 'Endpoint' — rename to '{fileName}Endpoint'"
                    );
            }
        }
    }
}
