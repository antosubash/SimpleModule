using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Pages;

public class ViewEndpoint : IViewEndpoint
{
    public const string Route = MapConstants.Routes.View;

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
                    "Map/View",
                    new
                    {
                        map,
                        sources = await sourcesTask,
                        basemaps = await basemapsTask,
                        defaultStyleUrl = options.Value.BaseStyleUrl,
                        enableMeasure = options.Value.EnableMeasureTools,
                        enableExportPng = options.Value.EnableExportPng,
                        enableGeolocate = options.Value.EnableGeolocate,
                    }
                );
            }
        );
    }
}
