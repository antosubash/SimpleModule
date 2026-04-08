using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Chat.Endpoints.Chat;

public class DeleteConversationEndpoint : IEndpoint
{
    public const string Route = ChatConstants.Routes.DeleteConversation;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                async (Guid id, ChatService service, ClaimsPrincipal user, CancellationToken ct) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                    await service.DeleteAsync(ConversationId.From(id), userId, ct);
                    return Results.NoContent();
                }
            )
            .RequirePermission(ChatPermissions.Create);
}
