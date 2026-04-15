using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public class CreateEndpoint : IEndpoint
{
    public const string Route = RateLimitingConstants.Routes.Create;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    CreateRateLimitRuleRequest request,
                    IValidator<CreateRateLimitRuleRequest> validator,
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

                    return await CrudEndpoints.Create(
                        () => contracts.CreateRuleAsync(request),
                        r => $"{RateLimitingConstants.RoutePrefix}/{r.Id}"
                    );
                }
            )
            .RequirePermission(RateLimitingPermissions.Create);
}
