using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}",
                (PageId id, IPageBuilderContracts pageBuilder) =>
                    CrudEndpoints.Delete(() => pageBuilder.DeletePageAsync(id))
            )
            .RequirePermission(PageBuilderPermissions.Delete);
}
