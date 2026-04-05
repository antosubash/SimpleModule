using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Templates;

public class CreateTemplateEndpoint : IEndpoint
{
    public const string Route = PageBuilderConstants.Routes.CreateTemplate;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    CreatePageTemplateRequest request,
                    IPageBuilderTemplateContracts templates
                ) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Name))
                        throw new ArgumentException("Template name is required.", nameof(request));

                    var template = await templates.CreateTemplateAsync(request);
                    return TypedResults.Created(
                        $"{PageBuilderConstants.RoutePrefix}/templates/{template.Id}",
                        template
                    );
                }
            )
            .RequirePermission(PageBuilderPermissions.Create);
}
