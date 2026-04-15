using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Chat;

namespace Chat.Tests.Integration;

public partial class ChatEndpointTests
{
    [Fact]
    public async Task ListConversations_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/chat/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListConversations_WithoutViewPermission_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient([ChatPermissions.Create]);

        var response = await client.GetAsync("/api/chat/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListConversations_WithViewPermission_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient([ChatPermissions.View]);

        var response = await client.GetAsync("/api/chat/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BrowseView_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/chat");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListConversations_OnlyReturnsCurrentUsersConversations()
    {
        var alice = _factory.CreateAuthenticatedClient(
            [ChatPermissions.View, ChatPermissions.Create],
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                "alice-list"
            )
        );
        var bob = _factory.CreateAuthenticatedClient(
            [ChatPermissions.View, ChatPermissions.Create],
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                "bob-list"
            )
        );

        await CreateConversationAsync(alice, "assistant", "alice-1");
        await CreateConversationAsync(alice, "assistant", "alice-2");
        await CreateConversationAsync(bob, "assistant", "bob-1");

        var aliceList = await alice.GetFromJsonAsync<List<JsonElement>>("/api/chat/conversations");

        aliceList.Should().NotBeNull();
        aliceList!
            .Select(e => e.GetProperty("title").GetString())
            .Should()
            .BeEquivalentTo(ExpectedAliceTitles);
    }
}
