using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class SolutionStructureCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        yield return File.Exists(solution.SlnxPath)
            ? new CheckResult("Solution file", CheckStatus.Pass, ".slnx exists")
            : new CheckResult("Solution file", CheckStatus.Fail, ".slnx not found");

        var requiredDirs = new[]
        {
            ("src/", Path.Combine(solution.RootPath, "src")),
            ("src/modules/", solution.ModulesPath),
            ("tests/", Path.Combine(solution.RootPath, "tests")),
        };

        foreach (var (name, path) in requiredDirs)
        {
            yield return Directory.Exists(path)
                ? new CheckResult($"Directory {name}", CheckStatus.Pass, "exists")
                : new CheckResult($"Directory {name}", CheckStatus.Fail, "missing");
        }
    }
}
