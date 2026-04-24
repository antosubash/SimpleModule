using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Items.Views;

public class WelcomeEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Inertia.Render("Items/Welcome", new { }))
            .ExcludeFromDescription()
            .AllowAnonymous();
    }
}
