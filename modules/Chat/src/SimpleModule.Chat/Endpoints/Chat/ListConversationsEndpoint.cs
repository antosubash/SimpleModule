using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Chat.Endpoints.Chat;

public class ListConversationsEndpoint : IEndpoint
{
    public const string Route = ChatConstants.Routes.ListConversations;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (IChatContracts chat, ClaimsPrincipal user, CancellationToken ct) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                    var conversations = await chat.GetUserConversationsAsync(userId, ct);
                    return Results.Ok(conversations);
                }
            )
            .RequirePermission(ChatPermissions.View);
}
