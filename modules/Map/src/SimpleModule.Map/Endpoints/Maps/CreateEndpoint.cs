using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Maps;

public class CreateMapEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.CreateMap;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                (CreateMapRequest request, IMapContracts map, IOptions<MapModuleOptions> options) =>
                {
                    var validation = CreateMapRequestValidator.Validate(
                        request,
                        options.Value.MaxLayersPerMap
                    );
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Create(
                        () => map.CreateMapAsync(request),
                        m => $"{MapConstants.RoutePrefix}/maps/{m.Id}"
                    );
                }
            )
            .RequirePermission(MapPermissions.Create);
}
