using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account;

public class ErrorEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.Error;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                (HttpContext context) =>
                {
                    var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
                    return Inertia.Render("Users/Account/Error", new { requestId });
                }
            )
            .AllowAnonymous();
    }
}
