using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.DefaultMap;

public class UpdateDefaultMapEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.UpdateDefaultMap;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (
                    UpdateDefaultMapRequest request,
                    IValidator<UpdateDefaultMapRequest> validator,
                    IMapContracts map
                ) =>
                {
                    var validation = await validator.ValidateAsync(request);
                    if (!validation.IsValid)
                    {
                        throw new Core.Exceptions.ValidationException(
                            validation.ToValidationErrors()
                        );
                    }

                    return await CrudEndpoints.Update(() => map.UpdateDefaultMapAsync(request));
                }
            )
            .RequirePermission(MapPermissions.Update);
}
