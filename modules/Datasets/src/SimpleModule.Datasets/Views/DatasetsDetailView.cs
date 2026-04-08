using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Views;

public class DatasetsDetailView : IViewEndpoint
{
    public const string Route = DatasetsConstants.Routes.Detail;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (Guid id, IDatasetsContracts datasets, CancellationToken ct) =>
                {
                    var dto = await datasets.GetByIdAsync(DatasetId.From(id), ct);
                    if (dto is null)
                    {
                        return Results.NotFound();
                    }
                    return Inertia.Render("Datasets/Detail", new { dataset = dto });
                }
            )
            .RequirePermission(DatasetsPermissions.View);
}
