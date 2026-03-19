using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Templates;

public class CreateTemplateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/templates",
                async (CreatePageTemplateRequest request, IPageBuilderContracts pageBuilder) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Name))
                        return Results.BadRequest("Template name is required.");

                    var template = await pageBuilder.CreateTemplateAsync(request);
                    return TypedResults.Created($"/api/pagebuilder/templates/{template.Id}", template);
                }
            )
            .RequirePermission(PageBuilderPermissions.Create);
}
