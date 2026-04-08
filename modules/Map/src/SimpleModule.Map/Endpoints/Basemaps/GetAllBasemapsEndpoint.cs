using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Basemaps;

public class GetAllBasemapsEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.GetAllBasemaps;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, (IMapContracts map) => CrudEndpoints.GetAll(map.GetAllBasemapsAsync))
            .RequirePermission(MapPermissions.View);
}
