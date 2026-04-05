using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class ClearHomePageEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.ClearHomePage;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                async (PublicMenuService service) =>
                {
                    await service.ClearHomePageAsync();
                    return TypedResults.NoContent();
                }
            )
            .RequireAuthorization();
}
