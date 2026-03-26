using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class NpmWorkspaceCheckTests : IDisposable
{
    private readonly string _tempDir;

    public NpmWorkspaceCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolution(string moduleName, string? rootPackageJson)
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var modulesDir = Path.Combine(_tempDir, "src", "modules");
        Directory.CreateDirectory(Path.Combine(modulesDir, moduleName));
        if (rootPackageJson is not null)
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), rootPackageJson);
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenModuleCoveredByWorkspaceGlob()
    {
        var solution = CreateSolution("Products", """
            {
              "workspaces": ["src/modules/*/src/*"]
            }
            """);
        var results = new NpmWorkspaceCheck().Run(solution).ToList();
        results.Should().ContainSingle(r => r.Name == "NpmWorkspace -> Products" && r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Fail_WhenModuleNotInWorkspaces()
    {
        var solution = CreateSolution("Products", """
            {
              "workspaces": ["packages/*"]
            }
            """);
        var results = new NpmWorkspaceCheck().Run(solution).ToList();
        results.Should().ContainSingle(r => r.Name == "NpmWorkspace -> Products" && r.Status == CheckStatus.Fail);
    }

    [Fact]
    public void Run_Warning_WhenRootPackageJsonMissing()
    {
        var solution = CreateSolution("Products", rootPackageJson: null);
        var results = new NpmWorkspaceCheck().Run(solution).ToList();
        results.Should().ContainSingle(r => r.Name == "NpmWorkspace -> Products" && r.Status == CheckStatus.Warning);
    }
}
