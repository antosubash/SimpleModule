using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Chat.Endpoints.Chat;

public class GetConversationEndpoint : IEndpoint
{
    public const string Route = ChatConstants.Routes.GetConversation;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (Guid id, ChatService service, ClaimsPrincipal user, CancellationToken ct) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                    var conversation = await service.GetConversationAsync(
                        ConversationId.From(id),
                        ct
                    );
                    if (
                        conversation is null
                        || !string.Equals(conversation.UserId, userId, StringComparison.Ordinal)
                    )
                    {
                        throw new NotFoundException("Conversation", id);
                    }
                    return Results.Ok(conversation);
                }
            )
            .RequirePermission(ChatPermissions.View);
}
