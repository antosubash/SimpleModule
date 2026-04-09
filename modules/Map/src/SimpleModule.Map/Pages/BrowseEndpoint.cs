using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Pages;

/// <summary>
/// Renders the singleton default map. Layer sources, basemaps and tool flags are
/// preloaded so the React page can show the map and let users add/remove overlays
/// in a single screen — there is no longer a list of saved maps.
/// </summary>
public class BrowseEndpoint : IViewEndpoint
{
    public const string Route = MapConstants.Routes.Browse;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (IMapContracts maps, IOptions<MapModuleOptions> options) =>
                {
                    var mapTask = maps.GetDefaultMapAsync();
                    var sourcesTask = maps.GetAllLayerSourcesAsync();
                    var basemapsTask = maps.GetAllBasemapsAsync();

                    return Inertia.Render(
                        "Map/Browse",
                        new
                        {
                            map = await mapTask,
                            sources = await sourcesTask,
                            basemaps = await basemapsTask,
                            defaultStyleUrl = options.Value.BaseStyleUrl,
                            maxLayers = options.Value.MaxLayersPerMap,
                            enableMeasure = options.Value.EnableMeasureTools,
                            enableExportPng = options.Value.EnableExportPng,
                            enableGeolocate = options.Value.EnableGeolocate,
                        }
                    );
                }
            )
            .AllowAnonymous();
    }
}
