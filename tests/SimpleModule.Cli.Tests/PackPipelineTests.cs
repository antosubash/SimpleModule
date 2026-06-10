using FluentAssertions;
using SimpleModule.Cli.Commands.Pack;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class PackPipelineTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        "sm-pack-tests-" + Guid.NewGuid().ToString("N")
    );

    public PackPipelineTests() => Directory.CreateDirectory(_tempDir);

    [Fact]
    public void ResolveModuleProjects_FindsImplContractsAndTests()
    {
        Touch("src/SimpleModule.X/SimpleModule.X.csproj");
        Touch("src/SimpleModule.X.Contracts/SimpleModule.X.Contracts.csproj");
        Touch("tests/SimpleModule.X.Tests/SimpleModule.X.Tests.csproj");

        var (projects, error) = PackPipeline.ResolveModuleProjects(_tempDir);

        error.Should().BeNull();
        projects!.AssemblyName.Should().Be("SimpleModule.X");
        projects.ContractsCsproj.Should().EndWith("SimpleModule.X.Contracts.csproj");
        projects.TestsCsproj.Should().EndWith("SimpleModule.X.Tests.csproj");
    }

    [Fact]
    public void ResolveModuleProjects_IgnoresObjDirectories()
    {
        Touch("src/SimpleModule.X/SimpleModule.X.csproj");
        Touch("src/SimpleModule.X/obj/Debug/SimpleModule.X.Stale.csproj");

        var (projects, error) = PackPipeline.ResolveModuleProjects(_tempDir);

        error.Should().BeNull();
        projects!.AssemblyName.Should().Be("SimpleModule.X");
    }

    [Fact]
    public void ResolveModuleProjects_FailsOnMultipleImplProjects()
    {
        Touch("src/SimpleModule.X/SimpleModule.X.csproj");
        Touch("src/SimpleModule.Y/SimpleModule.Y.csproj");

        var (projects, error) = PackPipeline.ResolveModuleProjects(_tempDir);

        projects.Should().BeNull();
        error.Should().Contain("2 candidate");
    }

    [Fact]
    public void ResolveModuleProjects_FailsOnMissingDirectory()
    {
        var (projects, error) = PackPipeline.ResolveModuleProjects(
            Path.Combine(_tempDir, "missing")
        );

        projects.Should().BeNull();
        error.Should().Contain("does not exist");
    }

    [Fact]
    public void ValidateManifest_NullManifest_ExplainsGeneratorWiring()
    {
        var errors = PackPipeline.ValidateManifest(null, "SimpleModule.X", _tempDir);

        errors.Should().ContainSingle().Which.Should().Contain("SimpleModuleProjectKind");
    }

    [Fact]
    public void ValidateManifest_IdMismatch_Fails()
    {
        var manifest = ModuleManifestData.TryParse(
            """{"schemaVersion":1,"id":"Wrong.Id","name":"X"}"""
        );

        var errors = PackPipeline.ValidateManifest(manifest, "SimpleModule.X", _tempDir);

        errors.Should().Contain(e => e.Contains("does not match the assembly name"));
    }

    [Fact]
    public void ValidateManifest_MissingFrontendBundle_Fails()
    {
        var manifest = ModuleManifestData.TryParse(
            """{"schemaVersion":1,"id":"SimpleModule.X","name":"X","frontendEntry":"_content/SimpleModule.X/SimpleModule.X.pages.js"}"""
        );

        var errors = PackPipeline.ValidateManifest(manifest, "SimpleModule.X", _tempDir);

        errors.Should().Contain(e => e.Contains("does not exist"));
    }

    [Fact]
    public void ValidateManifest_ValidManifestWithExistingBundle_Passes()
    {
        File.WriteAllText(Path.Combine(_tempDir, "SimpleModule.X.pages.js"), "export {};");
        var manifest = ModuleManifestData.TryParse(
            """{"schemaVersion":1,"id":"SimpleModule.X","name":"X","frontendEntry":"_content/SimpleModule.X/SimpleModule.X.pages.js"}"""
        );

        PackPipeline.ValidateManifest(manifest, "SimpleModule.X", _tempDir).Should().BeEmpty();
    }

    private void Touch(string relativePath)
    {
        var path = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "<Project />");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
