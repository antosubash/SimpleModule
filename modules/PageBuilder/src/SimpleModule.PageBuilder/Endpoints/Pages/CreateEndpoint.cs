using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class CreateEndpoint : IEndpoint
{
    public const string Route = PageBuilderConstants.Routes.Create;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    CreatePageRequest request,
                    IValidator<CreatePageRequest> validator,
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

                    return await CrudEndpoints.Create(
                        () => pageBuilder.CreatePageAsync(request),
                        p => $"{PageBuilderConstants.RoutePrefix}/{p.Id}"
                    );
                }
            )
            .RequirePermission(PageBuilderPermissions.Create);
}
