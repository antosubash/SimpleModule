using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Pages;

public class LayersEndpoint : IViewEndpoint
{
    public const string Route = MapConstants.Routes.Layers;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (IMapContracts maps) =>
                {
                    var sourcesTask = maps.GetAllLayerSourcesAsync();
                    var basemapsTask = maps.GetAllBasemapsAsync();
                    return Inertia.Render(
                        "Map/Layers",
                        new { sources = await sourcesTask, basemaps = await basemapsTask }
                    );
                }
            )
            .AllowAnonymous();
    }
}
