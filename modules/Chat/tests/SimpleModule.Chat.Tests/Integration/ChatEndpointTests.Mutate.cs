using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Chat;

namespace Chat.Tests.Integration;

public partial class ChatEndpointTests
{
    [Fact]
    public async Task RenameConversation_WhenOwner_UpdatesTitle()
    {
        var client = _factory.CreateAuthenticatedClient([
            ChatPermissions.View,
            ChatPermissions.Create,
        ]);
        var created = await CreateConversationAsync(client, "assistant", "Before");

        var renameResponse = await client.PatchAsJsonAsync(
            $"/api/chat/conversations/{created.Id.Value}",
            new { title = "After" }
        );

        renameResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await renameResponse.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("title").GetString().Should().Be("After");
    }

    [Fact]
    public async Task RenameConversation_WithEmptyTitle_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient([
            ChatPermissions.View,
            ChatPermissions.Create,
        ]);
        var created = await CreateConversationAsync(client, "assistant", "Before");

        var response = await client.PatchAsJsonAsync(
            $"/api/chat/conversations/{created.Id.Value}",
            new { title = "   " }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteConversation_WhenOwner_Returns204()
    {
        var client = _factory.CreateAuthenticatedClient([
            ChatPermissions.View,
            ChatPermissions.Create,
        ]);
        var created = await CreateConversationAsync(client, "assistant", "doomed");

        var response = await client.DeleteAsync($"/api/chat/conversations/{created.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteConversation_OtherUsers_Returns404()
    {
        var ownerClient = _factory.CreateAuthenticatedClient(
            [ChatPermissions.View, ChatPermissions.Create],
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                "alice"
            )
        );
        var created = await CreateConversationAsync(ownerClient, "assistant", "alices");

        var mallory = _factory.CreateAuthenticatedClient(
            [ChatPermissions.View, ChatPermissions.Create],
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                "mallory"
            )
        );

        var response = await mallory.DeleteAsync($"/api/chat/conversations/{created.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
