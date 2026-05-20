using FluentAssertions;
using SimpleModule.Cli.Commands.Maintenance;

namespace SimpleModule.Cli.Tests;

public sealed class MaintenanceSentinelTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "sm-maintenance-tests-" + Guid.NewGuid().ToString("N")
    );

    public MaintenanceSentinelTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Write_creates_sentinel_with_hashed_secret()
    {
        MaintenanceSentinel.Write(_root, secret: "open-sesame", message: "back at 5", retryAfterSeconds: 30, until: null);

        var path = MaintenanceSentinel.PathFor(_root);
        File.Exists(path).Should().BeTrue();

        var contents = File.ReadAllText(path);
        contents.Should().NotContain("open-sesame", because: "secret must never be written as plaintext");
        contents.Should().Contain(MaintenanceSentinel.HashSecret("open-sesame"));
        contents.Should().Contain("back at 5");
        contents.Should().Contain("30");
    }

    [Fact]
    public void Delete_removes_existing_sentinel()
    {
        MaintenanceSentinel.Write(_root, secret: null, message: null, retryAfterSeconds: 60, until: null);
        MaintenanceSentinel.Exists(_root).Should().BeTrue();

        var removed = MaintenanceSentinel.Delete(_root);

        removed.Should().BeTrue();
        MaintenanceSentinel.Exists(_root).Should().BeFalse();
    }

    [Fact]
    public void Delete_returns_false_when_absent()
    {
        MaintenanceSentinel.Delete(_root).Should().BeFalse();
    }

    [Fact]
    public void HashSecret_is_deterministic_and_lowercase_hex()
    {
        var a = MaintenanceSentinel.HashSecret("hello");
        var b = MaintenanceSentinel.HashSecret("hello");

        a.Should().Be(b);
        a.Should().MatchRegex("^[0-9a-f]+$");
    }
}
