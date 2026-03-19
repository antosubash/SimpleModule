using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Views;

public class ViewerEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/p/{slug}",
                async (string slug, IPageBuilderContracts pageBuilder) =>
                {
                    var page = await pageBuilder.GetPageBySlugAsync(slug);
                    if (page is null || !page.IsPublished)
                    {
                        return Results.NotFound();
                    }

                    return Inertia.Render("PageBuilder/Viewer", new { page });
                }
            )
            .AllowAnonymous();
    }
}
