using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.LayerSources;

public class CreateLayerSourceEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.CreateSource;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                (CreateLayerSourceRequest request, IMapContracts map) =>
                {
                    var validation = CreateLayerSourceRequestValidator.Validate(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Create(
                        () => map.CreateLayerSourceAsync(request),
                        s => $"{MapConstants.RoutePrefix}/sources/{s.Id}"
                    );
                }
            )
            .RequirePermission(MapPermissions.ManageSources);
}
