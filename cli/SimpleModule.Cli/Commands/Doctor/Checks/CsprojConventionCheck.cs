using System.Xml.Linq;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class CsprojConventionCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var moduleCsproj = Path.Combine(
                solution.GetModuleProjectPath(module),
                $"{module}.csproj"
            );
            if (File.Exists(moduleCsproj))
            {
                var doc = XDocument.Load(moduleCsproj);
                var root = doc.Root!;

                var hasFrameworkRef = root.Descendants("FrameworkReference")
                    .Any(fr => fr.Attribute("Include")?.Value == "Microsoft.AspNetCore.App");
                yield return hasFrameworkRef
                    ? new CheckResult(
                        $"{module} FrameworkRef",
                        CheckStatus.Pass,
                        "has Microsoft.AspNetCore.App"
                    )
                    : new CheckResult(
                        $"{module} FrameworkRef",
                        CheckStatus.Fail,
                        "missing FrameworkReference Microsoft.AspNetCore.App"
                    );
            }
            else
            {
                yield return new CheckResult(
                    $"{module} csproj",
                    CheckStatus.Fail,
                    $"{module}.csproj not found"
                );
            }
        }

        // Generator check
        var generatorCsproj = Path.Combine(
            solution.RootPath,
            "src",
            "SimpleModule.Generator",
            "SimpleModule.Generator.csproj"
        );
        if (File.Exists(generatorCsproj))
        {
            var doc = XDocument.Load(generatorCsproj);
            var targetFramework = doc.Root!.Descendants("TargetFramework").FirstOrDefault()?.Value;
            yield return targetFramework == "netstandard2.0"
                ? new CheckResult("Generator target", CheckStatus.Pass, "targets netstandard2.0")
                : new CheckResult(
                    "Generator target",
                    CheckStatus.Fail,
                    $"targets {targetFramework}, expected netstandard2.0"
                );
        }
    }
}
