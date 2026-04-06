using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class GetByIdEndpoint : IEndpoint
{
    public const string Route = PageBuilderConstants.Routes.GetById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (PageId id, IPageBuilderContracts pageBuilder) =>
                    CrudEndpoints.GetById(() => pageBuilder.GetPageByIdAsync(id))
            )
            .RequirePermission(PageBuilderPermissions.View);
}
