using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Menus;

public class GetAvailablePagesEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.GetAvailablePages;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, (IReadOnlyList<AvailablePage> pages) => TypedResults.Ok(pages))
            .RequireAuthorization();
}
