using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Exceptions;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public class UpdateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "/{id}",
            async Task<Results<Ok<Order>, NotFound, ValidationProblem>> (
                int id,
                UpdateOrderRequest request,
                IOrderContracts orderContracts
            ) =>
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

                var order = await orderContracts.UpdateOrderAsync(id, request);
                return TypedResults.Ok(order);
            }
        );
    }
}
