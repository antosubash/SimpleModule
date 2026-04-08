using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Agents;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Chat.Pages;

public class BrowseEndpoint : IViewEndpoint
{
    public const string Route = ChatConstants.Routes.Browse;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (
                    IChatContracts chat,
                    IAgentRegistry agents,
                    ClaimsPrincipal user,
                    CancellationToken ct
                ) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                    var conversations = await chat.GetUserConversationsAsync(userId, ct);
                    var agentList = agents
                        .GetAll()
                        .Select(a => new { name = a.Name, description = a.Description })
                        .ToArray();
                    return Inertia.Render("Chat/Browse", new { conversations, agents = agentList });
                }
            )
            .RequireAuthorization();
    }
}
