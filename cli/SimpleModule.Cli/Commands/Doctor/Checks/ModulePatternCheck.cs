using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class ModulePatternCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var moduleDir = solution.GetModuleProjectPath(module);

            var expectedFiles = new[]
            {
                ($"{module}Module.cs", "Module class"),
                ($"{module}Constants.cs", "Constants class"),
                ($"{module}DbContext.cs", "DbContext class"),
            };

            foreach (var (fileName, description) in expectedFiles)
            {
                var filePath = Path.Combine(moduleDir, fileName);
                yield return File.Exists(filePath)
                    ? new CheckResult(
                        $"{module}/{fileName}",
                        CheckStatus.Pass,
                        $"{description} exists"
                    )
                    : new CheckResult(
                        $"{module}/{fileName}",
                        CheckStatus.Warning,
                        $"{description} missing"
                    );
            }

            var endpointsDir = Path.Combine(moduleDir, "Endpoints");
            yield return Directory.Exists(endpointsDir)
                ? new CheckResult(
                    $"{module}/Endpoints/",
                    CheckStatus.Pass,
                    "Endpoints directory exists"
                )
                : new CheckResult(
                    $"{module}/Endpoints/",
                    CheckStatus.Warning,
                    "Endpoints directory missing"
                );
        }
    }
}
