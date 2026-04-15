using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Validation;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class AddHostEndpoint : IEndpoint
{
    public const string Route = TenantsConstants.Routes.Api.AddHost;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    TenantId id,
                    AddTenantHostRequest request,
                    IValidator<AddTenantHostRequest> validator,
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

                    var host = await contracts.AddHostAsync(id, request);
                    return TypedResults.Created(
                        $"{TenantsConstants.RoutePrefix}/{id}/hosts/{host.Id}",
                        host
                    );
                }
            )
            .RequirePermission(TenantsPermissions.ManageHosts);
}
