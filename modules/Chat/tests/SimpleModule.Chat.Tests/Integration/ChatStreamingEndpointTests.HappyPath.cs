// HttpClient instances come from WebApplicationFactory.CreateClient and are owned
// by the short-lived test-scoped factory; explicit disposal adds no value.
#pragma warning disable CA2000
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Chat;
using AiChatRole = Microsoft.Extensions.AI.ChatRole;

namespace Chat.Tests.Integration;

public partial class ChatStreamingEndpointTests
{
    [Fact]
    public async Task Stream_EmitsTanStackContentChunks()
    {
        using var factory = CreateFactoryWithFakes(["Hello", " ", "world"]);
        var client = CreateAuthenticated(factory, ChatPermissions.Create);
        var conversation = await CreateConversationAsync(client);

        using var response = await client.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.Id.Value}/stream",
            new { messages = new[] { new { role = "user", content = "say hi" } } }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");

        var body = await response.Content.ReadAsStringAsync();
        var frames = ParseSseFrames(body);

        // Three content chunks + one done chunk + final [DONE] sentinel.
        frames.Should().HaveCount(5);

        var contentChunks = frames.Take(3).Select(ParseJson).ToArray();
        contentChunks
            .Should()
            .AllSatisfy(f => f.GetProperty("type").GetString().Should().Be("content"));
        contentChunks
            .Select(f => f.GetProperty("delta").GetString())
            .Should()
            .Equal("Hello", " ", "world");
        // Content mirrors delta per chunk (clients accumulate via useChat).
        contentChunks[2].GetProperty("content").GetString().Should().Be("world");
        contentChunks
            .Select(f => f.GetProperty("role").GetString())
            .Should()
            .AllBeEquivalentTo("assistant");

        var done = ParseJson(frames[3]);
        done.GetProperty("type").GetString().Should().Be("done");
        done.GetProperty("finishReason").GetString().Should().Be("stop");

        frames[4].Should().Be("[DONE]");
    }

    [Fact]
    public async Task Stream_PersistsUserAndAssistantMessages()
    {
        using var factory = CreateFactoryWithFakes(["Reply"]);
        var client = CreateAuthenticated(factory, ChatPermissions.View, ChatPermissions.Create);
        var conversation = await CreateConversationAsync(client);

        var streamResponse = await client.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.Id.Value}/stream",
            new { messages = new[] { new { role = "user", content = "question" } } }
        );
        streamResponse.EnsureSuccessStatusCode();
        _ = await streamResponse.Content.ReadAsStringAsync();

        var historyResponse = await client.GetAsync(
            $"/api/chat/conversations/{conversation.Id.Value}/messages"
        );
        historyResponse.EnsureSuccessStatusCode();
        var messages = await historyResponse.Content.ReadFromJsonAsync<List<JsonElement>>(
            JsonOptions
        );

        messages.Should().NotBeNull();
        messages!.Should().HaveCount(2);
        messages[0].GetProperty("content").GetString().Should().Be("question");
        messages[1].GetProperty("content").GetString().Should().Be("Reply");
    }

    [Fact]
    public async Task Stream_ForwardsHistoryFromTanStackPayloadToChatClient()
    {
        var fakeClient = new RecordingChatClient(["ok"]);
        using var factory = CreateFactoryWithSpecificClient(fakeClient);
        var client = CreateAuthenticated(factory, ChatPermissions.Create);
        var conversation = await CreateConversationAsync(client);

        _ = await client.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.Id.Value}/stream",
            new
            {
                messages = new[]
                {
                    new { role = "user", content = "first turn" },
                    new { role = "assistant", content = "first reply" },
                    new { role = "user", content = "second turn" },
                },
            }
        );

        var captured = fakeClient.LastMessages;
        captured.Should().NotBeNull();
        // Expected order: [system, user(history), assistant(history), user(new)]
        var roles = captured!.Select(m => m.Role).ToArray();
        roles
            .Should()
            .Equal(AiChatRole.System, AiChatRole.User, AiChatRole.Assistant, AiChatRole.User);
        captured
            .Select(m => m.Text)
            .Should()
            .Equal("You are a test agent.", "first turn", "first reply", "second turn");
    }
}
