using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account;

public class LockoutEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.Lockout;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, () => Inertia.Render("Users/Account/Lockout")).AllowAnonymous();
    }
}
