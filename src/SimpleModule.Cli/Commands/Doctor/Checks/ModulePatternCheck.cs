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

            var featuresDir = Path.Combine(moduleDir, "Features");
            yield return Directory.Exists(featuresDir)
                ? new CheckResult(
                    $"{module}/Features/",
                    CheckStatus.Pass,
                    "Features directory exists"
                )
                : new CheckResult(
                    $"{module}/Features/",
                    CheckStatus.Warning,
                    "Features directory missing"
                );
        }
    }
}
