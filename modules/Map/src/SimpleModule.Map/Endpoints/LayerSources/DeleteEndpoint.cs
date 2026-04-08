using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.LayerSources;

public class DeleteLayerSourceEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.DeleteSource;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                (LayerSourceId id, IMapContracts map) =>
                    CrudEndpoints.Delete(() => map.DeleteLayerSourceAsync(id))
            )
            .RequirePermission(MapPermissions.ManageSources);
}
