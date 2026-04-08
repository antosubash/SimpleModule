using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Chat.Endpoints.Chat;

public class RenameConversationEndpoint : IEndpoint
{
    public const string Route = ChatConstants.Routes.RenameConversation;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPatch(
                Route,
                async (
                    Guid id,
                    RenameConversationRequest request,
                    ChatService service,
                    ClaimsPrincipal user,
                    CancellationToken ct
                ) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Title))
                    {
                        throw new ValidationException("Title is required.");
                    }
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                    var conversation = await service.RenameAsync(
                        ConversationId.From(id),
                        userId,
                        request.Title,
                        ct
                    );
                    return Results.Ok(conversation);
                }
            )
            .RequirePermission(ChatPermissions.Create);
}
