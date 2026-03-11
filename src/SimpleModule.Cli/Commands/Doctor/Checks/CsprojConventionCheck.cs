using System.Xml.Linq;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class CsprojConventionCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var moduleCsproj = Path.Combine(solution.GetModuleProjectPath(module), $"{module}.csproj");
            if (File.Exists(moduleCsproj))
            {
                var doc = XDocument.Load(moduleCsproj);
                var root = doc.Root!;

                var hasFrameworkRef = root.Descendants("FrameworkReference")
                    .Any(fr => fr.Attribute("Include")?.Value == "Microsoft.AspNetCore.App");
                yield return hasFrameworkRef
                    ? new CheckResult($"{module} FrameworkRef", CheckStatus.Pass, "has Microsoft.AspNetCore.App")
                    : new CheckResult($"{module} FrameworkRef", CheckStatus.Fail, "missing FrameworkReference Microsoft.AspNetCore.App");

                var hasPublishAot = root.Descendants("PublishAot").Any();
                yield return !hasPublishAot
                    ? new CheckResult($"{module} PublishAot", CheckStatus.Pass, "no PublishAot (correct)")
                    : new CheckResult($"{module} PublishAot", CheckStatus.Fail, "module should NOT have PublishAot");
            }
            else
            {
                yield return new CheckResult($"{module} csproj", CheckStatus.Fail, $"{module}.csproj not found");
            }

            var contractsCsproj = Path.Combine(solution.GetModuleContractsPath(module), $"{module}.Contracts.csproj");
            if (File.Exists(contractsCsproj))
            {
                var doc = XDocument.Load(contractsCsproj);
                var refs = doc.Root!.Descendants("ProjectReference")
                    .Select(pr => pr.Attribute("Include")?.Value ?? "")
                    .ToList();

                var onlyRefsCore = refs.All(r => r.Contains("Core", StringComparison.OrdinalIgnoreCase));
                yield return onlyRefsCore
                    ? new CheckResult($"{module}.Contracts refs", CheckStatus.Pass, "references Core only")
                    : new CheckResult($"{module}.Contracts refs", CheckStatus.Warning, "contracts project references more than just Core");
            }
        }

        // Generator check
        var generatorCsproj = Path.Combine(solution.RootPath, "src", "SimpleModule.Generator", "SimpleModule.Generator.csproj");
        if (File.Exists(generatorCsproj))
        {
            var doc = XDocument.Load(generatorCsproj);
            var targetFramework = doc.Root!.Descendants("TargetFramework").FirstOrDefault()?.Value;
            yield return targetFramework == "netstandard2.0"
                ? new CheckResult("Generator target", CheckStatus.Pass, "targets netstandard2.0")
                : new CheckResult("Generator target", CheckStatus.Fail, $"targets {targetFramework}, expected netstandard2.0");
        }
    }
}
