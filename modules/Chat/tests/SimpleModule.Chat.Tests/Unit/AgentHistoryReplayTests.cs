using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Agents;
using SimpleModule.Agents.Dtos;
using SimpleModule.Core.Agents;

namespace Chat.Tests.Unit;

/// <summary>
/// Verifies that <see cref="AgentChatService"/> replays <see cref="AgentChatRequest.History"/>
/// into the message list it sends to the underlying <c>IChatClient</c>. This is the framework
/// change made for the Chat module so multi-turn conversations have context.
/// </summary>
public sealed class AgentHistoryReplayTests
{
    [Fact]
    public async Task ChatAsync_InjectsHistoryBetweenSystemAndUserMessages()
    {
        var (service, capture) = CreateService();

        var response = await service.ChatAsync(
            "test-agent",
            new AgentChatRequest(
                "What about tomorrow?",
                History: new[]
                {
                    new AgentHistoryMessage("user", "What's the weather today?"),
                    new AgentHistoryMessage("assistant", "Sunny and 72."),
                }
            )
        );

        response.Message.Should().Be("ack");
        capture.Messages.Should().NotBeNull();
        var roles = capture.Messages!.Select(m => m.Role).ToArray();
        var texts = capture.Messages!.Select(m => m.Text).ToArray();

        // Expected order: [system, user(history), assistant(history), user(new)]
        roles.Should().Equal(ChatRole.System, ChatRole.User, ChatRole.Assistant, ChatRole.User);
        texts[0].Should().Be("You are a test agent.");
        texts[1].Should().Be("What's the weather today?");
        texts[2].Should().Be("Sunny and 72.");
        texts[3].Should().Be("What about tomorrow?");
    }

    [Fact]
    public async Task ChatAsync_SkipsWhitespaceHistoryEntries()
    {
        var (service, capture) = CreateService();

        await service.ChatAsync(
            "test-agent",
            new AgentChatRequest(
                "current",
                History: new[]
                {
                    new AgentHistoryMessage("user", "real"),
                    new AgentHistoryMessage("assistant", "   "),
                    new AgentHistoryMessage("user", ""),
                }
            )
        );

        // Only the non-empty turn should be replayed, plus the new user message.
        capture
            .Messages!.Select(m => m.Text)
            .Should()
            .Equal("You are a test agent.", "real", "current");
    }

    [Fact]
    public async Task ChatAsync_WithoutHistory_HasOnlySystemAndUser()
    {
        var (service, capture) = CreateService();

        await service.ChatAsync("test-agent", new AgentChatRequest("hello"));

        capture.Messages!.Select(m => m.Role).Should().Equal(ChatRole.System, ChatRole.User);
    }

    [Fact]
    public async Task ChatAsync_UnknownHistoryRoleDefaultsToUser()
    {
        var (service, capture) = CreateService();

        await service.ChatAsync(
            "test-agent",
            new AgentChatRequest(
                "current",
                History: new[] { new AgentHistoryMessage("random", "weird") }
            )
        );

        capture
            .Messages!.Select(m => m.Role)
            .Should()
            .Equal(ChatRole.System, ChatRole.User, ChatRole.User);
    }

    [Fact]
    public async Task ChatStreamAsync_ReplaysHistoryBeforeStreaming()
    {
        var (service, capture) = CreateService(streamedText: "pong");

        var collected = new List<string>();
        await foreach (
            var chunk in service.ChatStreamAsync(
                "test-agent",
                new AgentChatRequest(
                    "ping",
                    History: new[] { new AgentHistoryMessage("assistant", "earlier") }
                )
            )
        )
        {
            collected.Add(chunk);
        }

        string.Concat(collected).Should().Be("pong");
        capture
            .Messages!.Select(m => m.Text)
            .Should()
            .Equal("You are a test agent.", "earlier", "ping");
    }

    // ---------- helpers ----------

    private static (AgentChatService service, CapturingChatClient capture) CreateService(
        string textResponse = "ack",
        string? streamedText = null
    )
    {
        var client = new CapturingChatClient(textResponse, streamedText ?? textResponse);
        var registry = new FakeAgentRegistry(
            new AgentRegistration(
                Name: "test-agent",
                Description: "Test",
                ModuleName: "Chat.Tests",
                AgentDefinitionType: typeof(TestAgent),
                ToolProviderTypes: Array.Empty<Type>()
            )
        );
        var sp = new ServiceCollection().BuildServiceProvider();
        var options = Options.Create(new AgentOptions { EnableRag = false });
        var service = new AgentChatService(registry, client, sp, options);
        return (service, client);
    }

    private sealed class TestAgent : IAgentDefinition
    {
        public string Name => "test-agent";
        public string Description => "Test";
        public string Instructions => "You are a test agent.";
        public bool? EnableRag => false;
    }

    private sealed class FakeAgentRegistry(AgentRegistration registration) : IAgentRegistry
    {
        public IReadOnlyList<AgentRegistration> GetAll() => new[] { registration };

        public AgentRegistration? GetByName(string name) =>
            name == registration.Name ? registration : null;
    }

    private sealed class CapturingChatClient(string responseText, string streamText) : IChatClient
    {
        public IList<Microsoft.Extensions.AI.ChatMessage>? Messages { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            Messages = messages.ToList();
            var response = new ChatResponse(
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, responseText)
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
            Messages = messages.ToList();
            yield return new ChatResponseUpdate(ChatRole.Assistant, streamText);
            await Task.CompletedTask;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
