using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Users.Views.Account;

[ViewPage("Users/Account/ResetPasswordConfirmation")]
public class ResetPasswordConfirmationEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/ResetPasswordConfirmation",
                () => Inertia.Render("Users/Account/ResetPasswordConfirmation")
            )
            .AllowAnonymous();
    }
}
