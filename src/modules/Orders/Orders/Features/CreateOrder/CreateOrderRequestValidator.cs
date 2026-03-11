using System.Globalization;
using System.Text;
using SimpleModule.Core.Validation;
using SimpleModule.Orders;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.CreateOrder;

public static class CreateOrderRequestValidator
{
    private static readonly CompositeFormat _quantityMustBePositiveFormat = CompositeFormat.Parse(
        OrdersConstants.ValidationMessages.QuantityMustBePositiveFormat
    );

    public static ValidationResult Validate(CreateOrderRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            errors[OrdersConstants.Fields.UserId] =
            [
                OrdersConstants.ValidationMessages.UserIdRequired,
            ];
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            errors[OrdersConstants.Fields.Items] =
            [
                OrdersConstants.ValidationMessages.AtLeastOneItemRequired,
            ];
        }
        else
        {
            var itemErrors = new List<string>();
            for (var i = 0; i < request.Items.Count; i++)
            {
                if (request.Items[i].Quantity <= 0)
                {
                    itemErrors.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            _quantityMustBePositiveFormat,
                            i
                        )
                    );
                }
            }

            if (itemErrors.Count > 0)
            {
                errors[OrdersConstants.Fields.Items] = [.. itemErrors];
            }
        }

        return errors.Count > 0 ? ValidationResult.WithErrors(errors) : ValidationResult.Success;
    }
}
