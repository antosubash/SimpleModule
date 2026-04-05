using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Core.Validation;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class UpdateEndpoint : IEndpoint
{
    public const string Route = PageBuilderConstants.Routes.Update;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                (PageId id, UpdatePageRequest request, IPageBuilderContracts pageBuilder) =>
                {
                    var validation = new ValidationBuilder()
                        .AddErrorIf(
                            string.IsNullOrWhiteSpace(request.Title),
                            "Title",
                            "Page title is required."
                        )
                        .AddErrorIf(
                            string.IsNullOrWhiteSpace(request.Slug),
                            "Slug",
                            "Page slug is required."
                        )
                        .Build();

                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Update(() => pageBuilder.UpdatePageAsync(id, request));
                }
            )
            .RequirePermission(PageBuilderPermissions.Update);
}
