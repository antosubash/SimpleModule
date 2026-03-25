using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Templates;

public class GetAllTemplatesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/templates",
                async (IPageBuilderTemplateContracts templates) =>
                    TypedResults.Ok(await templates.GetAllTemplatesAsync())
            )
            .RequirePermission(PageBuilderPermissions.View);
}
