using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Users.Views.Account;

[ViewPage("Users/Account/ForgotPasswordConfirmation")]
public class ForgotPasswordConfirmationEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/ForgotPasswordConfirmation",
                () => Inertia.Render("Users/Account/ForgotPasswordConfirmation")
            )
            .AllowAnonymous();
    }
}
