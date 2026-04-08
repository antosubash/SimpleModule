using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Views;

public class DatasetsBrowseView : IViewEndpoint
{
    public const string Route = DatasetsConstants.Routes.Browse;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (IDatasetsContracts datasets, CancellationToken ct) =>
                {
                    var rows = await datasets.GetAllAsync(ct);
                    return Inertia.Render("Datasets/Browse", new { datasets = rows });
                }
            )
            .RequirePermission(DatasetsPermissions.View);
}
