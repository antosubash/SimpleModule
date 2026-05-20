using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public class UpdateEndpoint : IEndpoint
{
    public const string Route = RateLimitingConstants.Routes.Update;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (
                    int id,
                    UpdateRateLimitRuleRequest request,
                    IValidator<UpdateRateLimitRuleRequest> validator,
                    IRateLimitingContracts contracts
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
                        contracts.UpdateRuleAsync(RateLimitRuleId.From(id), request)
                    );
                }
            )
            .RequirePermission(RateLimitingPermissions.Update);
}
