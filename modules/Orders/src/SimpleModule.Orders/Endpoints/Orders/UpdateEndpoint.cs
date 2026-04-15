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

public class UpdateEndpoint : IEndpoint
{
    public const string Route = OrdersConstants.Routes.Update;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (
                    OrderId id,
                    UpdateOrderRequest request,
                    IValidator<CreateOrderRequest> validator,
                    IOrderContracts orderContracts
                ) =>
                {
                    var createRequest = new CreateOrderRequest
                    {
                        UserId = request.UserId,
                        Items = request.Items,
                    };
                    var validation = await validator.ValidateAsync(createRequest);
                    if (!validation.IsValid)
                    {
                        throw new Core.Exceptions.ValidationException(
                            validation.ToValidationErrors()
                        );
                    }

                    return await CrudEndpoints.Update(() =>
                        orderContracts.UpdateOrderAsync(id, request)
                    );
                }
            )
            .RequirePermission(OrdersPermissions.Update);
}
