using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class SlnxEntriesCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        if (!File.Exists(solution.SlnxPath))
        {
            yield return new CheckResult("Slnx entries", CheckStatus.Fail, ".slnx not found");
            yield break;
        }

        foreach (var module in solution.ExistingModules)
        {
            var hasEntry = SlnxManipulator.HasModuleEntry(solution.SlnxPath, module);
            yield return hasEntry
                ? new CheckResult($"Slnx -> {module}", CheckStatus.Pass, "folder entry exists in .slnx")
                : new CheckResult($"Slnx -> {module}", CheckStatus.Fail, "missing folder entry in .slnx");
        }
    }
}
