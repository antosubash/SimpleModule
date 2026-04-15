using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
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
                async (
                    PageId id,
                    UpdatePageRequest request,
                    IValidator<UpdatePageRequest> validator,
                    IPageBuilderContracts pageBuilder
                ) =>
                {
                    var validation = await validator.ValidateAsync(request);
                    if (!validation.IsValid)
                    {
                        throw new Core.Exceptions.ValidationException(
                            validation.ToValidationErrors()
                        );
                    }

                    return await CrudEndpoints.Update(() =>
                        pageBuilder.UpdatePageAsync(id, request)
                    );
                }
            )
            .RequirePermission(PageBuilderPermissions.Update);
}
