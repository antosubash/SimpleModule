using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.LayerSources;

public class GetLayerSourceByIdEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.GetSourceById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (LayerSourceId id, IMapContracts map) =>
                    CrudEndpoints.GetById(() => map.GetLayerSourceByIdAsync(id))
            )
            .RequirePermission(MapPermissions.ViewSources);
}
