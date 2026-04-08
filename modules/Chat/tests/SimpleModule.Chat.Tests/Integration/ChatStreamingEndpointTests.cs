// HttpClient instances come from WebApplicationFactory.CreateClient and are owned
// by the short-lived test-scoped factory; explicit disposal adds no value.
#pragma warning disable CA2000
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Agents;
using SimpleModule.Chat;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core.Agents;
using SimpleModule.Host;
using SimpleModule.Tests.Shared.Fixtures;
using AiChatRole = Microsoft.Extensions.AI.ChatRole;

namespace Chat.Tests.Integration;

[Collection(TestCollections.Integration)]
public class ChatStreamingEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly SimpleModuleWebApplicationFactory _baseFactory;

    public ChatStreamingEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _baseFactory = factory;
        // Force the base factory to initialize its in-memory SQLite database so that
        // schema exists on the shared connection before any delegate factory uses it.
        _ = factory.CreateAuthenticatedClient(Array.Empty<Claim>());
    }

    // ---------- validation ----------

    [Fact]
    public async Task Stream_MissingMessagesArray_Returns400()
    {
        using var factory = CreateFactoryWithFakes(["ignored"]);
        var client = CreateAuthenticated(factory, ChatPermissions.Create);
        var conversation = await CreateConversationAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.Id.Value}/stream",
            new { messages = Array.Empty<object>() }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Stream_OnlyAssistantMessages_Returns400()
    {
        using var factory = CreateFactoryWithFakes(["ignored"]);
        var client = CreateAuthenticated(factory, ChatPermissions.Create);
        var conversation = await CreateConversationAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.Id.Value}/stream",
            new { messages = new[] { new { role = "assistant", content = "hi" } } }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Stream_WhitespaceLatestUserMessage_Returns400()
    {
        using var factory = CreateFactoryWithFakes(["ignored"]);
        var client = CreateAuthenticated(factory, ChatPermissions.Create);
        var conversation = await CreateConversationAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.Id.Value}/stream",
            new { messages = new[] { new { role = "user", content = "   " } } }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Stream_NonExistentConversation_Returns404()
    {
        using var factory = CreateFactoryWithFakes(["x"]);
        var client = CreateAuthenticated(factory, ChatPermissions.Create);

        var response = await client.PostAsJsonAsync(
            $"/api/chat/conversations/{Guid.NewGuid()}/stream",
            new { messages = new[] { new { role = "user", content = "hi" } } }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stream_WithoutCreatePermission_Returns403()
    {
        using var factory = CreateFactoryWithFakes(["x"]);
        // Owner creates the conversation with Create permission.
        var ownerClient = CreateAuthenticatedAs(
            factory,
            "owner-user",
            ChatPermissions.View,
            ChatPermissions.Create
        );
        var conversation = await CreateConversationAsync(ownerClient);

        // Viewer with only View tries to send a message.
        var viewerClient = CreateAuthenticatedAs(factory, "viewer-user", ChatPermissions.View);
        var response = await viewerClient.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.Id.Value}/stream",
            new { messages = new[] { new { role = "user", content = "hi" } } }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- happy path: SSE wire format ----------

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

    // ---------- error handling ----------

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

    // ---------- helpers ----------

    private static async Task<Conversation> CreateConversationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/chat/conversations",
            new { agentName = "test-agent", title = "Streaming test" }
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Conversation>(JsonOptions))!;
    }

    private WebApplicationFactory<Program> CreateFactoryWithFakes(string[] tokens) =>
        CreateFactoryWithSpecificClient(new RecordingChatClient(tokens));

    private WebApplicationFactory<Program> CreateFactoryWithSpecificClient(
        IChatClient chatClient
    ) =>
        _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace IChatClient with our recording fake.
                var chatClientDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(IChatClient)
                );
                if (chatClientDescriptor is not null)
                {
                    services.Remove(chatClientDescriptor);
                }
                services.AddSingleton(chatClient);

                // Replace IAgentRegistry with a single test agent.
                var registryDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(IAgentRegistry)
                );
                if (registryDescriptor is not null)
                {
                    services.Remove(registryDescriptor);
                }
                var registry = new AgentRegistry();
                registry.Register(
                    new AgentRegistration(
                        Name: "test-agent",
                        Description: "Integration test agent",
                        ModuleName: "Chat.Tests",
                        AgentDefinitionType: typeof(StreamingTestAgent),
                        ToolProviderTypes: Array.Empty<Type>()
                    )
                );
                services.AddSingleton<IAgentRegistry>(registry);
            });
        });

    private static HttpClient CreateAuthenticated(
        WebApplicationFactory<Program> factory,
        params string[] permissions
    ) => CreateAuthenticatedAs(factory, "test-user-id", permissions);

    private static HttpClient CreateAuthenticatedAs(
        WebApplicationFactory<Program> factory,
        string userId,
        params string[] permissions
    )
    {
        var client = factory.CreateClient();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));
        var headerValue = string.Join(";", claims.Select(c => $"{c.Type}={c.Value}"));
        client.DefaultRequestHeaders.Add("X-Test-Claims", headerValue);
        return client;
    }

    private static List<string> ParseSseFrames(string body)
    {
        // Each SSE frame is "data: <payload>\n\n"
        var frames = new List<string>();
        foreach (
            var segment in body.Split(
                "\n\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
        )
        {
            if (segment.StartsWith("data: ", StringComparison.Ordinal))
            {
                frames.Add(segment[6..]);
            }
        }
        return frames;
    }

    private static JsonElement ParseJson(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    // ---------- fakes ----------

    internal sealed class StreamingTestAgent : IAgentDefinition
    {
        public string Name => "test-agent";
        public string Description => "Test";
        public string Instructions => "You are a test agent.";
        public bool? EnableRag => false;
    }

    internal sealed class RecordingChatClient(
        string[] tokens,
        int? throwAfterTokens = null,
        string throwMessage = "boom"
    ) : IChatClient
    {
        public IList<Microsoft.Extensions.AI.ChatMessage>? LastMessages { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            LastMessages = messages.ToList();
            var response = new ChatResponse(
                new Microsoft.Extensions.AI.ChatMessage(AiChatRole.Assistant, string.Concat(tokens))
            );
            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
                CancellationToken cancellationToken = default
        )
        {
            LastMessages = messages.ToList();
            var emitted = 0;
            foreach (var token in tokens)
            {
                if (throwAfterTokens is { } limit && emitted >= limit)
                {
                    throw new InvalidOperationException(throwMessage);
                }
                yield return new ChatResponseUpdate(AiChatRole.Assistant, token);
                emitted++;
            }
            if (throwAfterTokens == 0)
            {
                throw new InvalidOperationException(throwMessage);
            }
            await Task.CompletedTask;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
