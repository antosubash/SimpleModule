using System.Globalization;
using System.Text;
using SimpleModule.Core.Validation;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public static class CreateRequestValidator
{
    private static readonly CompositeFormat QuantityMustBePositiveFormat = CompositeFormat.Parse(
        OrdersConstants.ValidationMessages.QuantityMustBePositiveFormat
    );

    public static ValidationResult Validate(CreateOrderRequest request)
    {
        var builder = new ValidationBuilder()
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.UserId.Value),
                OrdersConstants.Fields.UserId,
                OrdersConstants.ValidationMessages.UserIdRequired
            )
            .AddErrorIf(
                request.Items is null || request.Items.Count == 0,
                OrdersConstants.Fields.Items,
                OrdersConstants.ValidationMessages.AtLeastOneItemRequired
            );

        if (request.Items is { Count: > 0 })
        {
            for (var i = 0; i < request.Items.Count; i++)
            {
                builder.AddErrorIf(
                    request.Items[i].Quantity <= 0,
                    OrdersConstants.Fields.Items,
                    string.Format(CultureInfo.InvariantCulture, QuantityMustBePositiveFormat, i)
                );
            }
        }

        return builder.Build();
    }
}
