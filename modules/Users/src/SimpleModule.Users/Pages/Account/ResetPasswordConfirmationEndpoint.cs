using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account;

public class ResetPasswordConfirmationEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.ResetPasswordConfirmation;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, () => Inertia.Render("Users/Account/ResetPasswordConfirmation"))
            .AllowAnonymous();
    }
}
