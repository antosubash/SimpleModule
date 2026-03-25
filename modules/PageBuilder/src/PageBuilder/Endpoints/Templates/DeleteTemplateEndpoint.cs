using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Templates;

public class DeleteTemplateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/templates/{id}",
                (PageTemplateId id, IPageBuilderTemplateContracts templates) =>
                    CrudEndpoints.Delete(() => templates.DeleteTemplateAsync(id))
            )
            .RequirePermission(PageBuilderPermissions.Delete);
}
