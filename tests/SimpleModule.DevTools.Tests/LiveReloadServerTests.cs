using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace SimpleModule.DevTools.Tests;

public sealed class LiveReloadServerTests : IDisposable
{
    private readonly LiveReloadServer _server = new(NullLogger<LiveReloadServer>.Instance);

    public void Dispose()
    {
        _server.Dispose();
    }

    [Fact]
    public async Task NotifyReloadAsync_Does_Not_Throw_When_No_Clients()
    {
        // Should be a no-op with zero connected clients
        await _server.NotifyReloadAsync(ReloadType.Full, "test");
    }

    [Fact]
    public async Task NotifyReloadAsync_Does_Not_Throw_For_CssOnly()
    {
        await _server.NotifyReloadAsync(ReloadType.CssOnly, "Tailwind");
    }

    [Fact]
    public void Dispose_Is_Idempotent()
    {
        var server = new LiveReloadServer(NullLogger<LiveReloadServer>.Instance);
        server.Dispose();
        server.Dispose();
    }
}

public sealed class ReloadMessageSerializationTests
{
    [Fact]
    public void ReloadMessage_Serializes_Full_Type()
    {
        var message = new ReloadMessage { Type = ReloadType.Full, Source = "Products" };
        var json = System.Text.Json.JsonSerializer.Serialize(
            message,
            LiveReloadJsonContext.Default.ReloadMessage
        );

        json.Should().Contain("\"type\":\"Full\"");
        json.Should().Contain("\"source\":\"Products\"");
    }

    [Fact]
    public void ReloadMessage_Serializes_CssOnly_Type()
    {
        var message = new ReloadMessage { Type = ReloadType.CssOnly, Source = "Tailwind" };
        var json = System.Text.Json.JsonSerializer.Serialize(
            message,
            LiveReloadJsonContext.Default.ReloadMessage
        );

        json.Should().Contain("\"type\":\"CssOnly\"");
        json.Should().Contain("\"source\":\"Tailwind\"");
    }

    [Fact]
    public void ReloadMessage_Deserializes_Roundtrip()
    {
        var original = new ReloadMessage { Type = ReloadType.Full, Source = "ClientApp" };
        var json = System.Text.Json.JsonSerializer.Serialize(
            original,
            LiveReloadJsonContext.Default.ReloadMessage
        );
        var deserialized = System.Text.Json.JsonSerializer.Deserialize(
            json,
            LiveReloadJsonContext.Default.ReloadMessage
        );

        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().Be(ReloadType.Full);
        deserialized.Source.Should().Be("ClientApp");
    }
}
