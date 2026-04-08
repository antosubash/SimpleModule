using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Maps;

public class GetAllMapsEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.GetAllMaps;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, (IMapContracts map) => CrudEndpoints.GetAll(map.GetAllMapsAsync))
            .RequirePermission(MapPermissions.View);
}
