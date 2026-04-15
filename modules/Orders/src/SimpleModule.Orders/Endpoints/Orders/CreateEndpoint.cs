using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.Orders.Contracts;
using OrdersConstants = SimpleModule.Orders.Contracts.OrdersConstants;

namespace SimpleModule.Orders.Endpoints.Orders;

public class CreateEndpoint : IEndpoint
{
    public const string Route = OrdersConstants.Routes.Create;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    CreateOrderRequest request,
                    IValidator<CreateOrderRequest> validator,
                    IOrderContracts orderContracts
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
                        () => orderContracts.CreateOrderAsync(request),
                        o => $"{OrdersConstants.RoutePrefix}/{o.Id}"
                    );
                }
            )
            .RequirePermission(OrdersPermissions.Create);
}
