using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Views;

public class EditorEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/pages/new",
                () => Inertia.Render("PageBuilder/Editor", new { page = (Page?)null })
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        app.MapGet(
                "/admin/pages/{id}/edit",
                async (PageId id, IPageBuilderContracts pageBuilder) =>
                {
                    var page = await pageBuilder.GetPageByIdAsync(id);
                    if (page is null)
                    {
                        return Results.NotFound();
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
