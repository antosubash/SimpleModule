using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class DeleteMenuItemEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/menus/{id}",
                async (int id, PublicMenuService service) =>
                {
                    var deleted = await service.DeleteAsync(id);
                    return deleted ? Results.NoContent() : Results.NotFound();
                }
            )
            .RequireAuthorization();
}
