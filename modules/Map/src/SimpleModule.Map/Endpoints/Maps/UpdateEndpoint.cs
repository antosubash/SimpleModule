using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Maps;

public class UpdateMapEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.UpdateMap;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                (
                    SavedMapId id,
                    UpdateMapRequest request,
                    IMapContracts map,
                    IOptions<MapModuleOptions> options
                ) =>
                {
                    var validation = UpdateMapRequestValidator.Validate(
                        request,
                        options.Value.MaxLayersPerMap
                    );
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Update(() => map.UpdateMapAsync(id, request));
                }
            )
            .RequirePermission(MapPermissions.Update);
}
