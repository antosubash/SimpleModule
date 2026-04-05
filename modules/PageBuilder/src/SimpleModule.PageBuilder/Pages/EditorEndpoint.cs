using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Pages;

public class EditorEndpoint : IViewEndpoint
{
    public const string Route = PageBuilderConstants.Routes.Editor;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (IPageBuilderTemplateContracts templates) =>
                    Inertia.Render(
                        "PageBuilder/Editor",
                        new
                        {
                            page = (Page?)null,
                            templates = await templates.GetAllTemplatesAsync(),
                        }
                    )
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        app.MapGet(
                PageBuilderConstants.Routes.EditPage,
                async (PageId id, IPageBuilderContracts pageBuilder) =>
                {
                    var page = await pageBuilder.GetPageByIdAsync(id);
                    if (page is null)
                    {
                        return TypedResults.NotFound();
                    }

                    // Editor works on draft content, falling back to published content
                    var editorPage = new Page
                    {
                        Id = page.Id,
                        Title = page.Title,
                        Slug = page.Slug,
                        Content = page.DraftContent ?? page.Content,
                        DraftContent = page.DraftContent,
                        IsPublished = page.IsPublished,
                        Order = page.Order,
                        CreatedAt = page.CreatedAt,
                        UpdatedAt = page.UpdatedAt,
                    };

                    return Inertia.Render("PageBuilder/Editor", new { page = editorPage });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
