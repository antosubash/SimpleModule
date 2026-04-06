using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Pages;

public class ViewerEndpoint : IViewEndpoint
{
    public const string Route = PageBuilderConstants.Routes.Viewer;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (string slug, IPageBuilderContracts pageBuilder) =>
                {
                    var page = await pageBuilder.GetPageBySlugAsync(slug);
                    if (page is null || !page.IsPublished)
                    {
                        return TypedResults.NotFound();
                    }

                    return Inertia.Render("PageBuilder/Viewer", new { page });
                }
            )
            .AllowAnonymous();
    }
}
