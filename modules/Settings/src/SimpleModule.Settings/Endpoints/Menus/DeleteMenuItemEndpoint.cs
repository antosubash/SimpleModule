using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class DeleteMenuItemEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.DeleteMenuItem;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                async Task<IResult> (int id, PublicMenuService service) =>
                {
                    var deleted = await service.DeleteAsync(PublicMenuItemId.From(id));
                    return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .RequireAuthorization();
}
