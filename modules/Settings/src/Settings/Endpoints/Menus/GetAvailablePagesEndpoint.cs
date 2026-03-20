using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Menu;

namespace SimpleModule.Settings.Endpoints.Menus;

public class GetAvailablePagesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/menus/available-pages",
                (IReadOnlyList<AvailablePage> pages) => Results.Ok(pages)
            )
            .RequireAuthorization();
}
