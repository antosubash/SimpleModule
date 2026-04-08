using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Endpoints.Datasets;

public class GetDatasetEndpoint : IEndpoint
{
    public const string Route = DatasetsConstants.Routes.GetById;
    public const string Method = "GET";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async Task<IResult> (Guid id, IDatasetsContracts datasets, CancellationToken ct) =>
                {
                    var dto = await datasets.GetByIdAsync(DatasetId.From(id), ct);
                    return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
                }
            )
            .RequirePermission(DatasetsPermissions.View);
}
