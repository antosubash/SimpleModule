using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Views;

public class DatasetsUploadView : IViewEndpoint
{
    public const string Route = DatasetsConstants.Routes.UploadView;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, () => Inertia.Render("Datasets/Upload"))
            .RequirePermission(DatasetsPermissions.Upload);
}
