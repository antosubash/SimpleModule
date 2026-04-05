using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Users.Views.Account;

[ViewPage("Users/Account/Error")]
public class ErrorEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/Error",
                (HttpContext context) =>
                {
                    var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
                    return Inertia.Render("Users/Account/Error", new { requestId });
                }
            )
            .AllowAnonymous();
    }
}
