using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class ProjectReferenceCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        if (!File.Exists(solution.ApiCsprojPath))
        {
            yield return new CheckResult(
                "API csproj",
                CheckStatus.Fail,
                "SimpleModule.Host.csproj not found"
            );
            yield break;
        }

        foreach (var module in solution.ExistingModules)
        {
            var hasRef = ProjectManipulator.HasProjectReference(solution.ApiCsprojPath, module);
            yield return hasRef
                ? new CheckResult($"API -> {module}", CheckStatus.Pass, "project reference exists")
                : new CheckResult(
                    $"API -> {module}",
                    CheckStatus.Fail,
                    "missing project reference in API csproj"
                );
        }
    }
}
