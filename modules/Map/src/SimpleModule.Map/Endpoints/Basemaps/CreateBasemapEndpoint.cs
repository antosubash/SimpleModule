using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Basemaps;

public class CreateBasemapEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.CreateBasemap;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    CreateBasemapRequest request,
                    IValidator<CreateBasemapRequest> validator,
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

                    return await CrudEndpoints.Create(
                        () => map.CreateBasemapAsync(request),
                        b => $"{MapConstants.RoutePrefix}/basemaps/{b.Id}"
                    );
                }
            )
            .RequirePermission(MapPermissions.ManageSources);
}
