using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Core.Validation;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/",
                (CreatePageRequest request, IPageBuilderContracts pageBuilder) =>
                {
                    var validation = new ValidationBuilder()
                        .AddErrorIf(
                            string.IsNullOrWhiteSpace(request.Title),
                            "Title",
                            "Page title is required."
                        )
                        .Build();

                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Create(
                        () => pageBuilder.CreatePageAsync(request),
                        p => $"{PageBuilderConstants.RoutePrefix}/{p.Id}"
                    );
                }
            )
            .RequirePermission(PageBuilderPermissions.Create);
}
