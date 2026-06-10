using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class SmConfigTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        "sm-config-tests-" + Guid.NewGuid().ToString("N")
    );

    public SmConfigTests() => Directory.CreateDirectory(_tempDir);

    [Fact]
    public void Load_NoFile_ReturnsNuGetOrgDefault()
    {
        var config = SmConfig.Load(_tempDir);

        config.Registry.Should().Be("https://api.nuget.org/v3/index.json");
    }

    [Fact]
    public void Load_FileWithRegistry_ReturnsConfiguredRegistry()
    {
        File.WriteAllText(
            Path.Combine(_tempDir, "sm.json"),
            """{"registry": "https://my-feed.example.com/v3/index.json"}"""
        );

        var config = SmConfig.Load(_tempDir);

        config.Registry.Should().Be("https://my-feed.example.com/v3/index.json");
    }

    [Fact]
    public void Load_FileWithoutRegistryField_FallsBackToDefault()
    {
        File.WriteAllText(Path.Combine(_tempDir, "sm.json"), """{"otherSetting": true}""");

        var config = SmConfig.Load(_tempDir);

        config.Registry.Should().Be("https://api.nuget.org/v3/index.json");
    }

    [Fact]
    public void Load_MalformedJson_FallsBackToDefault()
    {
        File.WriteAllText(Path.Combine(_tempDir, "sm.json"), "{not json");

        var config = SmConfig.Load(_tempDir);

        config.Registry.Should().Be("https://api.nuget.org/v3/index.json");
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var config = new SmConfig { Registry = "https://feed.example/v3/index.json" };
        config.Save(_tempDir);

        SmConfig.Load(_tempDir).Registry.Should().Be("https://feed.example/v3/index.json");
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
