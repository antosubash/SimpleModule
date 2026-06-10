using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class PackageReferenceManipulatorTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        "sm-pkgref-tests-" + Guid.NewGuid().ToString("N")
    );

    private readonly string _csprojPath;

    public PackageReferenceManipulatorTests()
    {
        Directory.CreateDirectory(_tempDir);
        _csprojPath = Path.Combine(_tempDir, "Demo.Host.csproj");
        File.WriteAllText(
            _csprojPath,
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="SimpleModule.Hosting" />
              </ItemGroup>
            </Project>
            """
        );
    }

    private void WriteCpmProps()
    {
        File.WriteAllText(
            Path.Combine(_tempDir, "Directory.Packages.props"),
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="SimpleModule.Core" Version="0.0.38" />
                <PackageVersion Include="SimpleModule.Hosting" Version="0.0.38" />
              </ItemGroup>
            </Project>
            """
        );
    }

    [Fact]
    public void Add_Cpm_WritesPackageVersionAndVersionlessReference()
    {
        WriteCpmProps();

        PackageReferenceManipulator.AddPackage(
            _csprojPath,
            _tempDir,
            "SimpleModule.FeatureFlags",
            "1.2.3"
        );

        File.ReadAllText(_csprojPath)
            .Should()
            .Contain("<PackageReference Include=\"SimpleModule.FeatureFlags\" />")
            .And.NotContain("SimpleModule.FeatureFlags\" Version=");
        File.ReadAllText(Path.Combine(_tempDir, "Directory.Packages.props"))
            .Should()
            .Contain("<PackageVersion Include=\"SimpleModule.FeatureFlags\" Version=\"1.2.3\" />");
    }

    [Fact]
    public void Add_NonCpm_WritesInlineVersion()
    {
        PackageReferenceManipulator.AddPackage(
            _csprojPath,
            _tempDir,
            "SimpleModule.FeatureFlags",
            "1.2.3"
        );

        File.ReadAllText(_csprojPath)
            .Should()
            .Contain(
                "<PackageReference Include=\"SimpleModule.FeatureFlags\" Version=\"1.2.3\" />"
            );
    }

    [Fact]
    public void Add_IsIdempotent()
    {
        WriteCpmProps();

        PackageReferenceManipulator.AddPackage(_csprojPath, _tempDir, "SimpleModule.X", "1.0.0");
        PackageReferenceManipulator.AddPackage(_csprojPath, _tempDir, "SimpleModule.X", "1.0.0");

        CountOccurrences(File.ReadAllText(_csprojPath), "Include=\"SimpleModule.X\"")
            .Should()
            .Be(1);
        CountOccurrences(
                File.ReadAllText(Path.Combine(_tempDir, "Directory.Packages.props")),
                "Include=\"SimpleModule.X\""
            )
            .Should()
            .Be(1);
    }

    [Fact]
    public void Add_Cpm_UpdatesExistingPackageVersion()
    {
        WriteCpmProps();
        PackageReferenceManipulator.AddPackage(_csprojPath, _tempDir, "SimpleModule.X", "1.0.0");

        PackageReferenceManipulator.AddPackage(_csprojPath, _tempDir, "SimpleModule.X", "2.0.0");

        var props = File.ReadAllText(Path.Combine(_tempDir, "Directory.Packages.props"));
        props.Should().Contain("Include=\"SimpleModule.X\" Version=\"2.0.0\"");
        props
            .Should()
            .NotContain("Version=\"1.0.0\" />\n    <PackageVersion Include=\"SimpleModule.X\"");
    }

    [Fact]
    public void Remove_DeletesReferenceAndCpmEntry()
    {
        WriteCpmProps();
        PackageReferenceManipulator.AddPackage(_csprojPath, _tempDir, "SimpleModule.X", "1.0.0");

        var removed = PackageReferenceManipulator.RemovePackage(
            _csprojPath,
            _tempDir,
            "SimpleModule.X"
        );

        removed.Should().BeTrue();
        File.ReadAllText(_csprojPath).Should().NotContain("SimpleModule.X");
        File.ReadAllText(Path.Combine(_tempDir, "Directory.Packages.props"))
            .Should()
            .NotContain("SimpleModule.X");
    }

    [Fact]
    public void Remove_MissingPackage_ReturnsFalse()
    {
        PackageReferenceManipulator.RemovePackage(_csprojPath, _tempDir, "Nope").Should().BeFalse();
    }

    [Fact]
    public void GetPackageReferences_ReturnsIdsWithResolvedCpmVersions()
    {
        WriteCpmProps();
        PackageReferenceManipulator.AddPackage(_csprojPath, _tempDir, "SimpleModule.X", "1.5.0");

        var references = PackageReferenceManipulator.GetPackageReferences(_csprojPath, _tempDir);

        references.Should().ContainSingle(r => r.Id == "SimpleModule.X" && r.Version == "1.5.0");
        references.Should().Contain(r => r.Id == "SimpleModule.Hosting" && r.Version == "0.0.38");
    }

    private static int CountOccurrences(string text, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
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
