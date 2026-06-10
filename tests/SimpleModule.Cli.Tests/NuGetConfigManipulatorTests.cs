using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class NuGetConfigManipulatorTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        "sm-nugetconfig-tests-" + Guid.NewGuid().ToString("N")
    );

    public NuGetConfigManipulatorTests() => Directory.CreateDirectory(_tempDir);

    [Fact]
    public void EnsureLocalSource_CreatesConfigWhenMissing()
    {
        var feedDir = Path.Combine(_tempDir, "feed");
        Directory.CreateDirectory(feedDir);

        NuGetConfigManipulator.EnsureLocalSource(_tempDir, feedDir);

        var configPath = Path.Combine(_tempDir, "nuget.config");
        File.Exists(configPath).Should().BeTrue();
        var content = File.ReadAllText(configPath);
        content.Should().Contain(feedDir);
        content.Should().Contain("nuget.org"); // public feed preserved
    }

    [Fact]
    public void EnsureLocalSource_AppendsToExistingConfig()
    {
        var configPath = Path.Combine(_tempDir, "nuget.config");
        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """
        );
        var feedDir = Path.Combine(_tempDir, "feed");
        Directory.CreateDirectory(feedDir);

        NuGetConfigManipulator.EnsureLocalSource(_tempDir, feedDir);

        var content = File.ReadAllText(configPath);
        content.Should().Contain(feedDir);
        content.Should().Contain("nuget.org");
    }

    [Fact]
    public void EnsureLocalSource_IsIdempotent()
    {
        var feedDir = Path.Combine(_tempDir, "feed");
        Directory.CreateDirectory(feedDir);

        NuGetConfigManipulator.EnsureLocalSource(_tempDir, feedDir);
        NuGetConfigManipulator.EnsureLocalSource(_tempDir, feedDir);

        var content = File.ReadAllText(Path.Combine(_tempDir, "nuget.config"));
        CountOf(content, feedDir).Should().Be(1);
    }

    private static int CountOf(string text, string token)
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
