using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.LayerSources;

public class UpdateLayerSourceEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.UpdateSource;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (
                    LayerSourceId id,
                    UpdateLayerSourceRequest request,
                    IValidator<UpdateLayerSourceRequest> validator,
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

                    return await CrudEndpoints.Update(() =>
                        map.UpdateLayerSourceAsync(id, request)
                    );
                }
            )
            .RequirePermission(MapPermissions.ManageSources);
}
