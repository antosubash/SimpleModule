using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class PackageJsonCheckTests : IDisposable
{
    private readonly string _tempDir;

    public PackageJsonCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolutionWithPackageJson(
        string moduleName,
        string? packageJsonContent
    )
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var moduleDir = Path.Combine(_tempDir, "src", "modules", moduleName, "src", moduleName);
        Directory.CreateDirectory(moduleDir);
        if (packageJsonContent is not null)
            File.WriteAllText(Path.Combine(moduleDir, "package.json"), packageJsonContent);
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenReactAndInertiaAreInPeerDeps()
    {
        var solution = CreateSolutionWithPackageJson(
            "Products",
            """
            {
              "peerDependencies": {
                "react": "^19.0.0",
                "@inertiajs/react": "^2.0.0"
              }
            }
            """
        );
        var results = new PackageJsonCheck().Run(solution).ToList();
        results
            .Should()
            .ContainSingle(r => r.Name == "Products package.json" && r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Warn_WhenReactIsInDependenciesNotPeerDeps()
    {
        var solution = CreateSolutionWithPackageJson(
            "Products",
            """
            {
              "dependencies": { "react": "^19.0.0" },
              "peerDependencies": { "@inertiajs/react": "^2.0.0" }
            }
            """
        );
        var results = new PackageJsonCheck().Run(solution).ToList();
        results
            .Should()
            .ContainSingle(r =>
                r.Name == "Products package.json" && r.Status == CheckStatus.Warning
            );
    }

    [Fact]
    public void Run_Warn_WhenPackageJsonMissing()
    {
        var solution = CreateSolutionWithPackageJson("Products", packageJsonContent: null);
        var results = new PackageJsonCheck().Run(solution).ToList();
        results
            .Should()
            .ContainSingle(r =>
                r.Name == "Products package.json" && r.Status == CheckStatus.Warning
            );
    }
}
