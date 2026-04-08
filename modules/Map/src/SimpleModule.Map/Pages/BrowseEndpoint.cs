using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Pages;

public class BrowseEndpoint : IViewEndpoint
{
    public const string Route = MapConstants.Routes.Browse;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (IMapContracts maps, IOptions<MapModuleOptions> options) =>
                    Inertia.Render(
                        "Map/Browse",
                        new
                        {
                            maps = await maps.GetAllMapsAsync(),
                            defaultStyleUrl = options.Value.BaseStyleUrl,
                        }
                    )
            )
            .AllowAnonymous();
    }
}
