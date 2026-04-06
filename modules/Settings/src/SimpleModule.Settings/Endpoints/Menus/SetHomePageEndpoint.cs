using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class SetHomePageEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.SetHomePage;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (int id, PublicMenuService service) =>
                {
                    await service.SetHomePageAsync(id);
                    return TypedResults.NoContent();
                }
            )
            .RequireAuthorization();
}
