using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class PagesRegistryCheckTests : IDisposable
{
    private readonly string _tempDir;

    public PagesRegistryCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolution(string moduleName, string[]? csFiles = null, string? pagesIndexContent = null)
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var moduleDir = Path.Combine(_tempDir, "src", "modules", moduleName, "src", moduleName);
        var pagesDir = Path.Combine(moduleDir, "Pages");
        Directory.CreateDirectory(pagesDir);

        if (csFiles is not null)
        {
            var endpointsDir = Path.Combine(moduleDir, "Endpoints", moduleName);
            Directory.CreateDirectory(endpointsDir);
            foreach (var (content, i) in csFiles.Select((c, i) => (c, i)))
                File.WriteAllText(Path.Combine(endpointsDir, $"Endpoint{i}.cs"), content);
        }

        if (pagesIndexContent is not null)
            File.WriteAllText(Path.Combine(pagesDir, "index.ts"), pagesIndexContent);

        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenAllInertiaCallsHavePageEntry()
    {
        var solution = CreateSolution("Products",
            csFiles: [@"Inertia.Render(""Products/Browse"", props)"],
            pagesIndexContent: """
                export const pages: Record<string, any> = {
                    "Products/Browse": () => import("../Views/Browse"),
                };
                """);
        var results = new PagesRegistryCheck().Run(solution).ToList();
        results.Should().ContainSingle(r => r.Name == "Pages -> Products/Browse" && r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Fail_WhenInertiaCallHasNoPageEntry()
    {
        var solution = CreateSolution("Products",
            csFiles: [@"Inertia.Render(""Products/Browse"", props)"],
            pagesIndexContent: "export const pages: Record<string, any> = {};");
        var results = new PagesRegistryCheck().Run(solution).ToList();
        results.Should().ContainSingle(r => r.Name == "Pages -> Products/Browse" && r.Status == CheckStatus.Fail);
    }

    [Fact]
    public void Run_Fail_WhenPagesIndexTsMissing()
    {
        var solution = CreateSolution("Products",
            csFiles: [@"Inertia.Render(""Products/Browse"", props)"],
            pagesIndexContent: null);
        var results = new PagesRegistryCheck().Run(solution).ToList();
        results.Should().ContainSingle(r => r.Name == "Pages -> Products/Browse" && r.Status == CheckStatus.Fail);
    }

    [Fact]
    public void Run_Pass_WhenNoInertiaCallsExist()
    {
        var solution = CreateSolution("Products",
            csFiles: ["// no inertia calls here"],
            pagesIndexContent: "export const pages: Record<string, any> = {};");
        var results = new PagesRegistryCheck().Run(solution).ToList();
        results.Should().BeEmpty();
    }
}
