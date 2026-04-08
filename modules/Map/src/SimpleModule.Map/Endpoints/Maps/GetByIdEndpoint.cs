using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Maps;

public class GetMapByIdEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.GetMapById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (SavedMapId id, IMapContracts map) =>
                    CrudEndpoints.GetById(() => map.GetMapByIdAsync(id))
            )
            .RequirePermission(MapPermissions.View);
}
