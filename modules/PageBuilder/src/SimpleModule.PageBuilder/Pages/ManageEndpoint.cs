using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Pages;

public class ManageEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/manage",
                async (IPageBuilderContracts pageBuilder) =>
                    Inertia.Render(
                        "PageBuilder/Manage",
                        new { pages = await pageBuilder.GetAllPagesAsync() }
                    )
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
