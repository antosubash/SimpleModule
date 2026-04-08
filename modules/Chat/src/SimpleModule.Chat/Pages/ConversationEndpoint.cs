using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Chat.Contracts;
using SimpleModule.Chat.Dtos;
using SimpleModule.Core;
using SimpleModule.Core.Exceptions;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Chat.Pages;

public class ConversationEndpoint : IViewEndpoint
{
    public const string Route = ChatConstants.Routes.Conversation;

    public void Map(IEndpointRouteBuilder app)
    {
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

                    var initialMessages = conversation
                        .Messages.Where(m => m.Role != ChatRole.System)
                        .Select(m => new UiMessage(
                            Id: m.Id.Value.ToString(),
                            Role: ChatRoleExtensions.ToWire(m.Role),
                            Parts: new[] { new UiMessagePart("text", m.Content) },
                            CreatedAt: m.CreatedAt
                        ))
                        .ToArray();

                    return Inertia.Render(
                        "Chat/Conversation",
                        new { conversation, initialMessages }
                    );
                }
            )
            .RequireAuthorization();
    }
}
