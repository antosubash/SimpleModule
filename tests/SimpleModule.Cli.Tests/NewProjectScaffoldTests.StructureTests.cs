using FluentAssertions;
using SimpleModule.Cli.Commands.New;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed partial class NewProjectScaffoldTests
{
    [Fact]
    public void Scaffold_CreatesExpectedFiles()
    {
        var (projectName, rootDir) = ScaffoldStandalone();

        File.Exists(Path.Combine(rootDir, $"{projectName}.slnx")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "Directory.Build.props")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "Directory.Packages.props")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "nuget.config")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "global.json")).Should().BeTrue();
        File.Exists(
                Path.Combine(rootDir, "src", $"{projectName}.Host", $"{projectName}.Host.csproj")
            )
            .Should()
            .BeTrue();
        File.Exists(Path.Combine(rootDir, "src", $"{projectName}.Host", "Program.cs"))
            .Should()
            .BeTrue();
        File.Exists(
                Path.Combine(rootDir, "src", "modules", "Items", "src", "Items", "Items.csproj")
            )
            .Should()
            .BeTrue();
        File.Exists(
                Path.Combine(
                    rootDir,
                    "src",
                    "modules",
                    "Items",
                    "src",
                    "Items.Contracts",
                    "Items.Contracts.csproj"
                )
            )
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Scaffold_DirectoryPackagesProps_UsesPublishedVersion()
    {
        var (_, rootDir) = ScaffoldStandalone();

        var content = File.ReadAllText(Path.Combine(rootDir, "Directory.Packages.props"));
        content.Should().Contain($"Version=\"{TestVersion}\"");
        content.Should().NotContain("0.1.0-local");
    }

    [Fact]
    public void Scaffold_NugetConfig_OnlyContainsNuGetOrg()
    {
        var (_, rootDir) = ScaffoldStandalone();

        var content = File.ReadAllText(Path.Combine(rootDir, "nuget.config"));
        content.Should().Contain("nuget.org");
        content.Should().NotContain("SimpleModule-Local");
        content.Should().NotContain("nupkg");
    }

    [Fact]
    public void Scaffold_PackageJson_UsesPublishedNpmPackages()
    {
        var (_, rootDir) = ScaffoldStandalone();

        var content = File.ReadAllText(Path.Combine(rootDir, "package.json"));
        content.Should().Contain($"\"@simplemodule/client\": \"^{TestVersion}\"");
        content.Should().Contain($"\"@simplemodule/ui\": \"^{TestVersion}\"");
        content.Should().Contain($"\"@simplemodule/theme-default\": \"^{TestVersion}\"");
        content.Should().NotContain("file:");
    }

    [Fact]
    public void Scaffold_WithSolution_UsesLocalPackages()
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            return;
        }

        const string projectName = "TestApp";
        var rootDir = Path.Combine(_tempDir, projectName);

        NewProjectCommand.ScaffoldProject(
            projectName,
            rootDir,
            solution,
            frameworkVersion: TestVersion
        );

        var content = File.ReadAllText(Path.Combine(rootDir, "package.json"));
        content.Should().Contain("file:");
    }
}
