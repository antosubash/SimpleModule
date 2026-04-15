using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.LayerSources;

public class CreateLayerSourceEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.CreateSource;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    CreateLayerSourceRequest request,
                    IValidator<CreateLayerSourceRequest> validator,
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
                        () => map.CreateLayerSourceAsync(request),
                        s => $"{MapConstants.RoutePrefix}/sources/{s.Id}"
                    );
                }
            )
            .RequirePermission(MapPermissions.ManageSources);
}
