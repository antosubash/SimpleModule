// HttpClient instances come from WebApplicationFactory.CreateClient and are owned
// by the short-lived test-scoped factory; explicit disposal adds no value.
#pragma warning disable CA2000
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Agents;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core.Agents;
using SimpleModule.Host;
using SimpleModule.Tests.Shared.Fixtures;
using AiChatRole = Microsoft.Extensions.AI.ChatRole;

namespace Chat.Tests.Integration;

[Collection(TestCollections.Integration)]
public partial class ChatStreamingEndpointTests
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
