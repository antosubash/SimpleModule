using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.LayerSources;

public class CreateFromDatasetEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.CreateSourceFromDataset;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                (
                    CreateLayerSourceFromDatasetRequest request,
                    IMapContracts map,
                    CancellationToken ct
                ) =>
                    CrudEndpoints.Create(
                        () => map.CreateLayerSourceFromDatasetAsync(request, ct),
                        s => $"{MapConstants.RoutePrefix}/sources/{s.Id}"
                    )
            )
            .RequirePermission(MapPermissions.ManageSources);
}
