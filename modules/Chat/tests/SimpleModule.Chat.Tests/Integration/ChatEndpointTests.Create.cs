using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Chat;

namespace Chat.Tests.Integration;

public partial class ChatEndpointTests
{
    [Fact]
    public async Task CreateConversation_WithCreatePermission_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient([ChatPermissions.Create]);

        var response = await client.PostAsJsonAsync(
            "/api/chat/conversations",
            new { agentName = "assistant", title = "My chat" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("title").GetString().Should().Be("My chat");
        json.GetProperty("agentName").GetString().Should().Be("assistant");
    }

    [Fact]
    public async Task CreateConversation_WithoutPermission_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient([ChatPermissions.View]);

        var response = await client.PostAsJsonAsync(
            "/api/chat/conversations",
            new { agentName = "assistant" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateConversation_WithoutAgentName_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient([ChatPermissions.Create]);

        var response = await client.PostAsJsonAsync(
            "/api/chat/conversations",
            new { agentName = "" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
