using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Basemaps;

public class GetBasemapByIdEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.GetBasemapById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (BasemapId id, IMapContracts map) =>
                    CrudEndpoints.GetById(() => map.GetBasemapByIdAsync(id))
            )
            .RequirePermission(MapPermissions.View);
}
