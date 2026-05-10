using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Extensions;
using SimpleModule.Core.Inertia;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.OpenIddict.Pages.OpenIddict.ActiveSessions;

public class ActiveSessionsEndpoint : IEndpoint
{
    public const string Route = "/Identity/Account/Manage/ActiveSessions";
    public const string Method = "GET";

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async Task<IResult> (
                    ClaimsPrincipal principal,
                    IOpenIddictSessionContracts sessionContracts
                ) =>
                {
                    var userId = principal.GetUserId()!;
                    var currentTokenId = ActiveSessionsHelpers.GetCurrentTokenId(principal);
                    var sessions = await sessionContracts.GetActiveSessionsForUserAsync(
                        userId,
                        currentTokenId
                    );

                    return Inertia.Render(
                        "OpenIddict/Account/Manage/ActiveSessions",
                        new { sessions }
                    );
                }
            )
            .RequireAuthorization()
            .ExcludeFromDescription();
    }
}
