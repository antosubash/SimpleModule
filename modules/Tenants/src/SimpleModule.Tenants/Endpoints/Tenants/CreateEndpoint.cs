using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/",
                (CreateTenantRequest request, ITenantContracts contracts) =>
                {
                    var validation = CreateRequestValidator.Validate(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Create(
                        () => contracts.CreateTenantAsync(request),
                        t => $"{TenantsConstants.RoutePrefix}/{t.Id}"
                    );
                }
            )
            .RequirePermission(TenantsPermissions.Create);
}
