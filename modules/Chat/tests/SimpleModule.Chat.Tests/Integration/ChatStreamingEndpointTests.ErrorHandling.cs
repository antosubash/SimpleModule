// HttpClient instances come from WebApplicationFactory.CreateClient and are owned
// by the short-lived test-scoped factory; explicit disposal adds no value.
#pragma warning disable CA2000
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Chat;

namespace Chat.Tests.Integration;

public partial class ChatStreamingEndpointTests
{
    [Fact]
    public async Task Stream_WhenChatClientThrowsImmediately_EmitsErrorChunk()
    {
        using var factory = CreateFactoryWithSpecificClient(
            new RecordingChatClient(["ignored"], throwAfterTokens: 0, throwMessage: "upstream down")
        );
        var client = CreateAuthenticated(factory, ChatPermissions.Create);
        var conversation = await CreateConversationAsync(client);

        using var response = await client.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.Id.Value}/stream",
            new { messages = new[] { new { role = "user", content = "hi" } } }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var frames = ParseSseFrames(body);

        // Expect: error chunk, then [DONE].
        frames.Should().HaveCount(2);
        var error = ParseJson(frames[0]);
        error.GetProperty("type").GetString().Should().Be("error");
        error.GetProperty("error").GetProperty("message").GetString().Should().Be("upstream down");
        error.GetProperty("error").GetProperty("code").GetString().Should().Be("agent_error");
        frames[1].Should().Be("[DONE]");
    }

    [Fact]
    public async Task Stream_WhenChatClientThrowsMidway_EmitsContentThenError()
    {
        using var factory = CreateFactoryWithSpecificClient(
            new RecordingChatClient(
                ["Hello", " world", "!"],
                throwAfterTokens: 2,
                throwMessage: "network blip"
            )
        );
        var client = CreateAuthenticated(factory, ChatPermissions.View, ChatPermissions.Create);
        var conversation = await CreateConversationAsync(client);

        using var response = await client.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.Id.Value}/stream",
            new { messages = new[] { new { role = "user", content = "hi" } } }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var frames = ParseSseFrames(await response.Content.ReadAsStringAsync());

        // Expect: 2 content chunks, 1 error chunk, 1 [DONE].
        frames.Should().HaveCount(4);
        var firstContent = ParseJson(frames[0]);
        firstContent.GetProperty("type").GetString().Should().Be("content");
        firstContent.GetProperty("delta").GetString().Should().Be("Hello");
        var secondContent = ParseJson(frames[1]);
        secondContent.GetProperty("delta").GetString().Should().Be(" world");
        var error = ParseJson(frames[2]);
        error.GetProperty("type").GetString().Should().Be("error");
        error.GetProperty("error").GetProperty("message").GetString().Should().Be("network blip");
        frames[3].Should().Be("[DONE]");

        // The partial assistant reply should still be persisted so the user can see
        // what the model produced before failing.
        var history = await client.GetAsync(
            $"/api/chat/conversations/{conversation.Id.Value}/messages"
        );
        var messages = await history.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOptions);
        messages.Should().NotBeNull();
        messages!.Should().HaveCount(2);
        messages[1].GetProperty("content").GetString().Should().Be("Hello world");
    }
}
