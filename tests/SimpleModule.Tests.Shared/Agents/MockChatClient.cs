using Microsoft.Extensions.AI;

namespace SimpleModule.Tests.Shared.Agents;

public sealed class MockChatClient : IChatClient
{
    private readonly Queue<string> _responses = new();

    public MockChatClient(params string[] responses)
    {
        foreach (var r in responses)
            _responses.Enqueue(r);
    }

    public void EnqueueResponse(string response) => _responses.Enqueue(response);

    public ChatClientMetadata Metadata { get; } = new("MockChatClient");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var text = _responses.Count > 0 ? _responses.Dequeue() : "Mock response";
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default
    )
    {
        var text = _responses.Count > 0 ? _responses.Dequeue() : "Mock response";
        foreach (var word in text.Split(' '))
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new TextContent(word + " ")],
            };
            await Task.Yield();
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
