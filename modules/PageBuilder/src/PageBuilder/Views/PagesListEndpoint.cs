using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Views;

public class PagesListEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/pages",
                async (IPageBuilderContracts pageBuilder) =>
                    Inertia.Render(
                        "PageBuilder/PagesList",
                        new { pages = await pageBuilder.GetPublishedPagesAsync() }
                    )
            )
            .AllowAnonymous();
    }
}
