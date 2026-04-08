using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Endpoints.Datasets;

public class ListDatasetsEndpoint : IEndpoint
{
    public const string Route = DatasetsConstants.Routes.GetAll;
    public const string Method = "GET";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (IDatasetsContracts datasets, CancellationToken ct) =>
                    await datasets.GetAllAsync(ct)
            )
            .RequirePermission(DatasetsPermissions.View);
}
