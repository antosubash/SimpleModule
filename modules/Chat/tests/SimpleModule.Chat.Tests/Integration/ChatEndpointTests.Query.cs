using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Chat;
using SimpleModule.Chat.Contracts;

namespace Chat.Tests.Integration;

public partial class ChatEndpointTests
{
    [Fact]
    public async Task GetConversation_WhenOwner_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient([
            ChatPermissions.View,
            ChatPermissions.Create,
        ]);
        var created = await CreateConversationAsync(client, "assistant", "Owned");

        var response = await client.GetAsync($"/api/chat/conversations/{created.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("title").GetString().Should().Be("Owned");
    }

    [Fact]
    public async Task GetConversation_WhenMissing_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient([ChatPermissions.View]);

        var response = await client.GetAsync($"/api/chat/conversations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConversation_OtherUsersConversation_Returns404()
    {
        var ownerClient = _factory.CreateAuthenticatedClient(
            [ChatPermissions.View, ChatPermissions.Create],
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                "user-a"
            )
        );
        var created = await CreateConversationAsync(ownerClient, "assistant", "Private");

        var intruderClient = _factory.CreateAuthenticatedClient(
            [ChatPermissions.View],
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                "user-b"
            )
        );

        var response = await intruderClient.GetAsync($"/api/chat/conversations/{created.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMessages_NewConversation_ReturnsEmptyArray()
    {
        var client = _factory.CreateAuthenticatedClient([
            ChatPermissions.View,
            ChatPermissions.Create,
        ]);
        var created = await CreateConversationAsync(client, "assistant", "empty");

        var response = await client.GetAsync(
            $"/api/chat/conversations/{created.Id.Value}/messages"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var messages = await response.Content.ReadFromJsonAsync<List<ChatMessage>>(JsonOptions);
        messages.Should().NotBeNull().And.BeEmpty();
    }
}
