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

                    return Inertia.Render("PageBuilder/Editor", new { page });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
