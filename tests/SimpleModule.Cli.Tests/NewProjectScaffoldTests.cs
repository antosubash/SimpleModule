using System.Diagnostics;
using FluentAssertions;
using SimpleModule.Cli.Commands.New;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

[Trait("Category", "Integration")]
public sealed class NewProjectScaffoldTests : IDisposable
{
    private const string TestVersion = "0.0.15";
    private readonly string _tempDir;

    public NewProjectScaffoldTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-scaffold-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void Scaffold_CreatesExpectedFiles()
    {
        var (projectName, rootDir) = ScaffoldStandalone();

        File.Exists(Path.Combine(rootDir, $"{projectName}.slnx")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "Directory.Build.props")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "Directory.Packages.props")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "nuget.config")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "global.json")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "src", $"{projectName}.Host", $"{projectName}.Host.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "src", $"{projectName}.Host", "Program.cs")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "src", "modules", "Items", "src", "Items", "Items.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "src", "modules", "Items", "src", "Items.Contracts", "Items.Contracts.csproj")).Should().BeTrue();
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

        NewProjectCommand.ScaffoldProject(projectName, rootDir, solution, frameworkVersion: TestVersion);

        var content = File.ReadAllText(Path.Combine(rootDir, "package.json"));
        content.Should().Contain("file:");
    }

    [Fact]
    public void ScaffoldedProject_DotnetBuildSucceeds()
    {
        var frameworkVersion = NuGetVersionResolver.ResolveVersion();
        const string projectName = "TestApp";
        var rootDir = Path.Combine(_tempDir, projectName);

        NewProjectCommand.ScaffoldProject(projectName, rootDir, solution: null, frameworkVersion: frameworkVersion);

        var slnxPath = Path.Combine(rootDir, $"{projectName}.slnx");
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{slnxPath}\"",
            WorkingDirectory = rootDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(TimeSpan.FromSeconds(120));

        var buildOutput = $"""
            === STDOUT ===
            {stdout}
            === STDERR ===
            {stderr}
            """;

        process.ExitCode.Should().Be(0, $"dotnet build should succeed.\n{buildOutput}");
    }

    private (string ProjectName, string RootDir) ScaffoldStandalone(string projectName = "TestApp")
    {
        var rootDir = Path.Combine(_tempDir, projectName);
        NewProjectCommand.ScaffoldProject(projectName, rootDir, solution: null, frameworkVersion: TestVersion);
        return (projectName, rootDir);
    }
}
