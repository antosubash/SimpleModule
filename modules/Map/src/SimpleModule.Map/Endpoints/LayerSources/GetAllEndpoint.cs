using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.LayerSources;

public class GetAllLayerSourcesEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.GetAllSources;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, (IMapContracts map) => CrudEndpoints.GetAll(map.GetAllLayerSourcesAsync))
            .RequirePermission(MapPermissions.ViewSources);
}
