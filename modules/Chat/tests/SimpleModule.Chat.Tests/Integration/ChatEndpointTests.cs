using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Chat;
using SimpleModule.Chat.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Chat.Tests.Integration;

[Collection(TestCollections.Integration)]
public class ChatEndpointTests
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

    // ---------- authentication / authorization ----------

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

    // ---------- create ----------

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

    // ---------- get ----------

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

    // ---------- rename ----------

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

    // ---------- delete ----------

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

    // ---------- messages endpoint ----------

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

    // ---------- browse view ----------

    [Fact]
    public async Task BrowseView_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/chat");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- user isolation across requests ----------

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

    // ---------- helpers ----------

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
