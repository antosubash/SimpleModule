using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Maps;

public class DeleteMapEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.DeleteMap;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                (SavedMapId id, IMapContracts map) =>
                    CrudEndpoints.Delete(() => map.DeleteMapAsync(id))
            )
            .RequirePermission(MapPermissions.Delete);
}
