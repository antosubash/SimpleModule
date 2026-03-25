using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class UpdateMenuItemEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/menus/{id}",
                async Task<IResult> (int id, UpdateMenuItemRequest request, PublicMenuService service) =>
                {
                    var entity = await service.UpdateAsync(id, request);
                    return entity is not null ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .RequireAuthorization();
}
