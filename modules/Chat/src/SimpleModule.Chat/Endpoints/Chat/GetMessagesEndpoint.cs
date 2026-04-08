using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Chat.Endpoints.Chat;

public class GetMessagesEndpoint : IEndpoint
{
    public const string Route = ChatConstants.Routes.GetMessages;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (Guid id, ChatService service, ClaimsPrincipal user, CancellationToken ct) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                    var messages = await service.GetMessagesAsync(
                        ConversationId.From(id),
                        userId,
                        ct
                    );
                    return Results.Ok(messages);
                }
            )
            .RequirePermission(ChatPermissions.View);
}
