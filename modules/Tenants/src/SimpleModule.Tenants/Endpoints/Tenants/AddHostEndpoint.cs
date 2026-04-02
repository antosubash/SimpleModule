using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Exceptions;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class AddHostEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/{id}/hosts",
                async (TenantId id, AddTenantHostRequest request, ITenantContracts contracts) =>
                {
                    var validation = AddHostRequestValidator.Validate(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    var host = await contracts.AddHostAsync(id, request);
                    return TypedResults.Created(
                        $"{TenantsConstants.RoutePrefix}/{id}/hosts/{host.Id}",
                        host
                    );
                }
            )
            .RequirePermission(TenantsPermissions.ManageHosts);
}
