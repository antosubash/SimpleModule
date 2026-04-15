// HttpClient instances come from WebApplicationFactory.CreateClient and are owned
// by the short-lived test-scoped factory; explicit disposal adds no value.
#pragma warning disable CA2000
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Chat;

namespace Chat.Tests.Integration;

public partial class ChatStreamingEndpointTests
{
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
}
