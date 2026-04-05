using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
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
                (OrderId id, UpdateOrderRequest request, IOrderContracts orderContracts) =>
                {
                    var createRequest = new CreateOrderRequest
                    {
                        UserId = request.UserId,
                        Items = request.Items,
                    };
                    var validation = CreateRequestValidator.Validate(createRequest);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Update(() => orderContracts.UpdateOrderAsync(id, request));
                }
            )
            .RequirePermission(OrdersPermissions.Update);
}
