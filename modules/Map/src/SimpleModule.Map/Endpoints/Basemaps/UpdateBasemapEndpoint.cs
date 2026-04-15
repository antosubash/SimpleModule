using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Basemaps;

public class UpdateBasemapEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.UpdateBasemap;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (
                    BasemapId id,
                    UpdateBasemapRequest request,
                    IValidator<UpdateBasemapRequest> validator,
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

                    return await CrudEndpoints.Update(() => map.UpdateBasemapAsync(id, request));
                }
            )
            .RequirePermission(MapPermissions.ManageSources);
}
