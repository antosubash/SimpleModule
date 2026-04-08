using System.Text.Json;
using FluentAssertions;
using SimpleModule.Chat.Dtos;

namespace Chat.Tests.Unit;

public sealed class TanStackDtoSerializationTests
{
    private static readonly JsonSerializerOptions CamelCaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void TanStackContentChunk_SerializesWithExpectedFieldNames()
    {
        var chunk = new TanStackContentChunk(
            Type: "content",
            Id: "msg-1",
            Model: "assistant",
            Timestamp: 1700000000000L,
            Delta: "Hello",
            Content: "Hello",
            Role: "assistant"
        );

        var json = JsonSerializer.Serialize(chunk);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("type").GetString().Should().Be("content");
        root.GetProperty("id").GetString().Should().Be("msg-1");
        root.GetProperty("model").GetString().Should().Be("assistant");
        root.GetProperty("timestamp").GetInt64().Should().Be(1700000000000L);
        root.GetProperty("delta").GetString().Should().Be("Hello");
        root.GetProperty("content").GetString().Should().Be("Hello");
        root.GetProperty("role").GetString().Should().Be("assistant");
    }

    [Fact]
    public void TanStackDoneChunk_SerializesWithFinishReason()
    {
        var chunk = new TanStackDoneChunk(
            Type: "done",
            Id: "msg-1",
            Model: "assistant",
            Timestamp: 1700000000000L,
            FinishReason: "stop"
        );

        var json = JsonSerializer.Serialize(chunk);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("type").GetString().Should().Be("done");
        root.GetProperty("finishReason").GetString().Should().Be("stop");
        root.TryGetProperty("finish_reason", out _)
            .Should()
            .BeFalse("field name must be camelCase");
    }

    [Fact]
    public void UiMessage_SerializesPartsArray()
    {
        var message = new UiMessage(
            Id: "m-1",
            Role: "assistant",
            Parts: new[] { new UiMessagePart("text", "Hello world") },
            CreatedAt: DateTimeOffset.FromUnixTimeMilliseconds(1700000000000L)
        );

        var json = JsonSerializer.Serialize(message);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("id").GetString().Should().Be("m-1");
        root.GetProperty("role").GetString().Should().Be("assistant");
        var parts = root.GetProperty("parts");
        parts.GetArrayLength().Should().Be(1);
        parts[0].GetProperty("type").GetString().Should().Be("text");
        parts[0].GetProperty("content").GetString().Should().Be("Hello world");
    }

    [Fact]
    public void TanStackChatRequest_DeserializesFromTanStackWireShape()
    {
        const string json = """
            {
              "messages": [
                { "role": "user", "content": "Hi" },
                { "role": "assistant", "content": "Hello" },
                { "role": "user", "content": "How are you?" }
              ],
              "data": { "temperature": 0.5 }
            }
            """;

        var request = JsonSerializer.Deserialize<TanStackChatRequest>(json, CamelCaseInsensitive);

        request.Should().NotBeNull();
        request!.Messages.Should().HaveCount(3);
        request.Messages[0].Role.Should().Be("user");
        request.Messages[0].Content.Should().Be("Hi");
        request.Messages[2].Content.Should().Be("How are you?");
        request.Data.Should().NotBeNull();
        request.Data!.Should().ContainKey("temperature");
    }

    [Fact]
    public void TanStackChatRequest_DeserializesWithMissingData()
    {
        const string json = """
            { "messages": [{ "role": "user", "content": "Hi" }] }
            """;

        var request = JsonSerializer.Deserialize<TanStackChatRequest>(json, CamelCaseInsensitive);

        request.Should().NotBeNull();
        request!.Messages.Should().HaveCount(1);
        request.Data.Should().BeNull();
    }

    [Fact]
    public void ContentChunk_FrameFormatIsSseCompliant()
    {
        var chunk = new TanStackContentChunk(
            Type: "content",
            Id: "msg-1",
            Model: "m",
            Timestamp: 0,
            Delta: "x",
            Content: "x",
            Role: "assistant"
        );
        var frame = $"data: {JsonSerializer.Serialize(chunk)}\n\n";

        frame.Should().StartWith("data: ");
        frame.Should().EndWith("\n\n");
        frame.Should().NotContain("\n\ndata"); // only one frame
    }
}
