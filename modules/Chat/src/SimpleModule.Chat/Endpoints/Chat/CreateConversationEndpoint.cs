using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Chat.Endpoints.Chat;

public class CreateConversationEndpoint : IEndpoint
{
    public const string Route = ChatConstants.Routes.CreateConversation;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    CreateConversationRequest request,
                    IChatContracts chat,
                    ClaimsPrincipal user,
                    CancellationToken ct
                ) =>
                {
                    if (string.IsNullOrWhiteSpace(request.AgentName))
                    {
                        throw new ValidationException("AgentName is required.");
                    }
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                    var conversation = await chat.StartConversationAsync(
                        userId,
                        request.AgentName,
                        request.Title,
                        ct
                    );
                    return Results.Created(
                        $"{ChatConstants.RoutePrefix}/conversations/{conversation.Id.Value}",
                        conversation
                    );
                }
            )
            .RequirePermission(ChatPermissions.Create);
}
