using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class NuGetClientTests : IDisposable
{
    private readonly string _feedDir = Path.Combine(
        Path.GetTempPath(),
        "sm-nugetclient-tests-" + Guid.NewGuid().ToString("N")
    );

    public NuGetClientTests() => Directory.CreateDirectory(_feedDir);

    [Fact]
    public void FindLocalNupkg_PicksRequestedVersion()
    {
        Touch("SimpleModule.X.1.0.0.nupkg");
        Touch("SimpleModule.X.1.2.0.nupkg");

        var path = NuGetClient.FindLocalNupkg(_feedDir, "SimpleModule.X", "1.0.0");

        path.Should().EndWith("SimpleModule.X.1.0.0.nupkg");
    }

    [Fact]
    public void FindLocalNupkg_NoVersion_PicksHighest()
    {
        Touch("SimpleModule.X.1.0.0.nupkg");
        Touch("SimpleModule.X.1.10.0.nupkg");
        Touch("SimpleModule.X.1.2.0.nupkg");

        var path = NuGetClient.FindLocalNupkg(_feedDir, "SimpleModule.X", version: null);

        path.Should().EndWith("SimpleModule.X.1.10.0.nupkg");
    }

    [Fact]
    public void FindLocalNupkg_DoesNotMatchLongerPackageIds()
    {
        Touch("SimpleModule.X.Contracts.1.0.0.nupkg");

        NuGetClient.FindLocalNupkg(_feedDir, "SimpleModule.X", null).Should().BeNull();
    }

    [Fact]
    public void FindLocalNupkg_MissingPackage_ReturnsNull()
    {
        NuGetClient.FindLocalNupkg(_feedDir, "Nope", null).Should().BeNull();
    }

    [Fact]
    public void ExtractVersionFromFileName_HandlesPrerelease()
    {
        Touch("SimpleModule.X.0.0.39-local.nupkg");

        var path = NuGetClient.FindLocalNupkg(_feedDir, "SimpleModule.X", "0.0.39-local");

        path.Should().NotBeNull();
    }

    [Fact]
    public void IsLocalDirectorySource_DetectsPathsVsUrls()
    {
        NuGetClient.IsLocalDirectorySource(_feedDir).Should().BeTrue();
        NuGetClient
            .IsLocalDirectorySource("https://api.nuget.org/v3/index.json")
            .Should()
            .BeFalse();
    }

    private void Touch(string name) => File.WriteAllText(Path.Combine(_feedDir, name), "");

    public void Dispose()
    {
        try
        {
            Directory.Delete(_feedDir, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
