using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Basemaps;

public class CreateBasemapEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.CreateBasemap;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                (CreateBasemapRequest request, IMapContracts map) =>
                {
                    var validation = CreateBasemapRequestValidator.Validate(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Create(
                        () => map.CreateBasemapAsync(request),
                        b => $"{MapConstants.RoutePrefix}/basemaps/{b.Id}"
                    );
                }
            )
            .RequirePermission(MapPermissions.ManageSources);
}
