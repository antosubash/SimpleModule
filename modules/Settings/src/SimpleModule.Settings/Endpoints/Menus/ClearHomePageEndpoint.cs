using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class ClearHomePageEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/menus/home",
                async (PublicMenuService service) =>
                {
                    await service.ClearHomePageAsync();
                    return TypedResults.NoContent();
                }
            )
            .RequireAuthorization();
}
