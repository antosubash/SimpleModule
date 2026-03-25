using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Templates;

public class DeleteTemplateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/templates/{id}",
                async (PageTemplateId id, IPageBuilderTemplateContracts templates) =>
                {
                    await templates.DeleteTemplateAsync(id);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(PageBuilderPermissions.Delete);
}
