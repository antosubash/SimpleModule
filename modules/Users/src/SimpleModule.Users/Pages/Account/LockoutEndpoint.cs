using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Users.Pages.Account;

public class LockoutEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/Lockout", () => Inertia.Render("Users/Account/Lockout")).AllowAnonymous();
    }
}
