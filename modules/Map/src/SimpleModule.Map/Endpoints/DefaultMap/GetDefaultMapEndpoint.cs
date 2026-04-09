using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.DefaultMap;

public class GetDefaultMapEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.GetDefaultMap;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (IMapContracts map) => TypedResults.Ok(await map.GetDefaultMapAsync())
            )
            .RequirePermission(MapPermissions.View);
}
