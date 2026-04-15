using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class UpdateEndpoint : IEndpoint
{
    public const string Route = TenantsConstants.Routes.Api.Update;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (
                    TenantId id,
                    UpdateTenantRequest request,
                    IValidator<UpdateTenantRequest> validator,
                    ITenantContracts contracts
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
                        contracts.UpdateTenantAsync(id, request)
                    );
                }
            )
            .RequirePermission(TenantsPermissions.Update);
}
