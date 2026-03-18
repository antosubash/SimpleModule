using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", (CreateOrderRequest request, IOrderContracts orderContracts) =>
        {
            var validation = CreateRequestValidator.Validate(request);
            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            return CrudEndpoints.Create(
                () => orderContracts.CreateOrderAsync(request),
                o => $"{OrdersConstants.RoutePrefix}/{o.Id}");
        });
}
