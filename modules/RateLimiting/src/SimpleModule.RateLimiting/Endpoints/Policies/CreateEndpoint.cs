using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public class CreateEndpoint : IEndpoint
{
    public const string Route = RateLimitingConstants.Routes.Create;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                (CreateRateLimitRuleRequest request, IRateLimitingContracts contracts) =>
                {
                    var validation = CreateRequestValidator.Validate(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Create(
                        () => contracts.CreateRuleAsync(request),
                        r => $"{RateLimitingConstants.RoutePrefix}/{r.Id}"
                    );
                }
            )
            .RequirePermission(RateLimitingPermissions.Create);
}
