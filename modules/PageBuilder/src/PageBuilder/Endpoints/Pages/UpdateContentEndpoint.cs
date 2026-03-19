using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class UpdateContentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/{id}/content",
                (PageId id, UpdatePageContentRequest request, IPageBuilderContracts pageBuilder) =>
                    CrudEndpoints.Update(() =>
                        pageBuilder.UpdatePageContentAsync(id, request)
                    )
            )
            .RequirePermission(PageBuilderPermissions.Update);
}
