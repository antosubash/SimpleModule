using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class HostFrameworkVersionResolverTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        "sm-hostver-tests-" + Guid.NewGuid().ToString("N")
    );

    public HostFrameworkVersionResolverTests() => Directory.CreateDirectory(_tempDir);

    [Fact]
    public void Resolve_FromCpmPackageVersion()
    {
        File.WriteAllText(
            Path.Combine(_tempDir, "Directory.Packages.props"),
            """
            <Project>
              <ItemGroup>
                <PackageVersion Include="SimpleModule.Core" Version="0.0.42" />
              </ItemGroup>
            </Project>
            """
        );

        HostFrameworkVersionResolver.Resolve(_tempDir).Should().Be("0.0.42");
    }

    [Fact]
    public void Resolve_FallsBackToVersionJson()
    {
        File.WriteAllText(Path.Combine(_tempDir, "version.json"), """{"version": "0.0.7"}""");

        HostFrameworkVersionResolver.Resolve(_tempDir).Should().Be("0.0.7");
    }

    [Fact]
    public void Resolve_NothingFound_ReturnsNull()
    {
        HostFrameworkVersionResolver.Resolve(_tempDir).Should().BeNull();
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
