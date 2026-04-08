using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Endpoints.Datasets;

public class DeleteDatasetEndpoint : IEndpoint
{
    public const string Route = DatasetsConstants.Routes.Delete;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                async Task<IResult> (Guid id, IDatasetsContracts datasets, CancellationToken ct) =>
                {
                    await datasets.DeleteAsync(DatasetId.From(id), ct);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(DatasetsPermissions.Delete);
}
