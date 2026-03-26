using System.Xml.Linq;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class ContractsIsolationCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var contractsCsproj = Path.Combine(
                solution.GetModuleContractsPath(module),
                $"{module}.Contracts.csproj"
            );

            if (!File.Exists(contractsCsproj))
            {
                yield return new CheckResult(
                    $"{module}.Contracts isolation",
                    CheckStatus.Warning,
                    $"{module}.Contracts.csproj not found"
                );
                continue;
            }

            var doc = XDocument.Load(contractsCsproj);
            var nonCoreRefs = doc.Root!
                .Descendants("ProjectReference")
                .Select(pr => pr.Attribute("Include")?.Value ?? "")
                .Where(r => !r.Contains("Core", StringComparison.OrdinalIgnoreCase)
                         && !r.Contains("Contracts", StringComparison.OrdinalIgnoreCase))
                .ToList();

            yield return nonCoreRefs.Count == 0
                ? new CheckResult($"{module}.Contracts isolation", CheckStatus.Pass, "references Core only")
                : new CheckResult($"{module}.Contracts isolation", CheckStatus.Fail,
                    $"references non-Core projects: {string.Join(", ", nonCoreRefs.Select(Path.GetFileNameWithoutExtension))}");
        }
    }
}
