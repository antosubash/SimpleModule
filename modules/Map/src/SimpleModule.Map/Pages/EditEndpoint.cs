using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Pages;

public class EditEndpoint : IViewEndpoint
{
    public const string Route = MapConstants.Routes.Edit;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            Route,
            async (SavedMapId id, IMapContracts maps, IOptions<MapModuleOptions> options) =>
            {
                var mapTask = maps.GetMapByIdAsync(id);
                var sourcesTask = maps.GetAllLayerSourcesAsync();
                var basemapsTask = maps.GetAllBasemapsAsync();

                var map = await mapTask;
                if (map is null)
                {
                    return TypedResults.NotFound();
                }

                return Inertia.Render(
                    "Map/Edit",
                    new
                    {
                        map,
                        sources = await sourcesTask,
                        basemaps = await basemapsTask,
                        defaultStyleUrl = options.Value.BaseStyleUrl,
                        maxLayers = options.Value.MaxLayersPerMap,
                    }
                );
            }
        );
    }
}
