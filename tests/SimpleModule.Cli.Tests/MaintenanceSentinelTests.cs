using System.Text.Json;
using FluentAssertions;
using SimpleModule.Cli.Commands.Maintenance;

namespace SimpleModule.Cli.Tests;

public sealed class MaintenanceSentinelTests
{
    [Fact]
    public void HashSecret_is_deterministic_lowercase_hex()
    {
        var a = MaintenanceSentinelFile.HashSecret("opensesame");
        var b = MaintenanceSentinelFile.HashSecret("opensesame");

        a.Should().Be(b);
        a.Should().HaveLength(64);
        a.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void HashSecret_differs_per_secret()
    {
        var a = MaintenanceSentinelFile.HashSecret("alpha");
        var b = MaintenanceSentinelFile.HashSecret("beta");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Sentinel_round_trips_through_json()
    {
        var sentinel = new MaintenanceSentinel
        {
            SecretHash = MaintenanceSentinelFile.HashSecret("ssh"),
            Message = "Migrating",
            RetryAfterSeconds = 120,
            Until = DateTimeOffset.UtcNow.AddMinutes(5),
        };

        var json = JsonSerializer.Serialize(sentinel, MaintenanceSentinelFile.JsonOptions);
        var roundTripped = JsonSerializer.Deserialize<MaintenanceSentinel>(
            json,
            MaintenanceSentinelFile.JsonOptions
        );

        roundTripped.Should().BeEquivalentTo(sentinel);
        json.Should().Contain("secretHash");
        json.Should().Contain("retryAfterSeconds");
    }

    [Fact]
    public void TryRead_returns_null_when_file_absent()
    {
        var bogus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");

        MaintenanceSentinelFile.TryRead(bogus).Should().BeNull();
    }

    [Fact]
    public void TryRead_returns_null_for_corrupt_file()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        File.WriteAllText(path, "{not valid json");
        try
        {
            MaintenanceSentinelFile.TryRead(path).Should().BeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
