using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Users.Views.Account;

[ViewPage("Users/Account/AccessDenied")]
public class AccessDeniedEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/AccessDenied", () => Inertia.Render("Users/Account/AccessDenied"))
            .AllowAnonymous();
    }
}
