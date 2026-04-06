using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class UpdateEndpoint : IEndpoint
{
    public const string Route = TenantsConstants.Routes.Api.Update;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                (TenantId id, UpdateTenantRequest request, ITenantContracts contracts) =>
                {
                    var validation = UpdateRequestValidator.Validate(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Update(() => contracts.UpdateTenantAsync(id, request));
                }
            )
            .RequirePermission(TenantsPermissions.Update);
}
