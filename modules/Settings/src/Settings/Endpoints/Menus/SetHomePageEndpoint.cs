using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class SetHomePageEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/menus/{id}/home",
                async (int id, PublicMenuService service) =>
                {
                    await service.SetHomePageAsync(id);
                    return TypedResults.NoContent();
                }
            )
            .RequireAuthorization();
}
