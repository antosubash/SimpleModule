using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class GetMenuTreeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/menus",
                async (PublicMenuService service) =>
                {
                    var items = await service.GetAllAsync();
                    return TypedResults.Ok(items);
                }
            )
            .RequireAuthorization();
}
