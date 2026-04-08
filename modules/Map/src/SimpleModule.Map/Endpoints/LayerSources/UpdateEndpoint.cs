using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.LayerSources;

public class UpdateLayerSourceEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.UpdateSource;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                (LayerSourceId id, UpdateLayerSourceRequest request, IMapContracts map) =>
                {
                    var validation = UpdateLayerSourceRequestValidator.Validate(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Update(() => map.UpdateLayerSourceAsync(id, request));
                }
            )
            .RequirePermission(MapPermissions.ManageSources);
}
