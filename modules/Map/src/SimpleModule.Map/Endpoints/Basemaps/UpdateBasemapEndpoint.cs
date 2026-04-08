using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Basemaps;

public class UpdateBasemapEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.UpdateBasemap;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                (BasemapId id, UpdateBasemapRequest request, IMapContracts map) =>
                {
                    var validation = UpdateBasemapRequestValidator.Validate(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Update(() => map.UpdateBasemapAsync(id, request));
                }
            )
            .RequirePermission(MapPermissions.ManageSources);
}
