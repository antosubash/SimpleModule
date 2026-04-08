using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Basemaps;

public class DeleteBasemapEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.DeleteBasemap;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                (BasemapId id, IMapContracts map) =>
                    CrudEndpoints.Delete(() => map.DeleteBasemapAsync(id))
            )
            .RequirePermission(MapPermissions.ManageSources);
}
