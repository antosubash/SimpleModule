using System.Net.Http.Json;
using System.Text.Json;
using SimpleModule.Chat.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Chat.Tests.Integration;

[Collection(TestCollections.Integration)]
public partial class ChatEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly string[] ExpectedAliceTitles = ["alice-1", "alice-2"];

    private readonly SimpleModuleWebApplicationFactory _factory;

    public ChatEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Conversation> CreateConversationAsync(
        HttpClient client,
        string agentName,
        string title
    )
    {
        var response = await client.PostAsJsonAsync(
            "/api/chat/conversations",
            new { agentName, title }
        );
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Conversation>(JsonOptions);
        return created!;
    }
}
