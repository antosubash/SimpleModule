using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Pages;

public class ViewerDraftEndpoint : IViewEndpoint
{
    public const string Route = PageBuilderConstants.Routes.ViewerDraft;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (string slug, IPageBuilderContracts pageBuilder) =>
                {
                    var page = await pageBuilder.GetPageBySlugAsync(slug);
                    if (page is null)
                    {
                        return TypedResults.NotFound();
                    }

                    var viewerPage = new Page
                    {
                        Id = page.Id,
                        Title = page.Title,
                        Slug = page.Slug,
                        Content = page.DraftContent ?? page.Content,
                        IsPublished = page.IsPublished,
                        Order = page.Order,
                        CreatedAt = page.CreatedAt,
                        UpdatedAt = page.UpdatedAt,
                    };

                    return Inertia.Render(
                        "PageBuilder/ViewerDraft",
                        new { page = viewerPage, isDraft = true }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
