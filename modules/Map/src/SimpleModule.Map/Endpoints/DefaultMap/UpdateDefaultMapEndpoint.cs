using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.DefaultMap;

public class UpdateDefaultMapEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.UpdateDefaultMap;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                (
                    UpdateDefaultMapRequest request,
                    IMapContracts map,
                    IOptions<MapModuleOptions> options
                ) =>
                {
                    var validation = UpdateDefaultMapRequestValidator.Validate(
                        request,
                        options.Value.MaxLayersPerMap
                    );
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Update(() => map.UpdateDefaultMapAsync(request));
                }
            )
            .RequirePermission(MapPermissions.Update);
}
