using SimpleModule.Core.Validation;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.CreateOrder;

public static class CreateOrderRequestValidator
{
    public static ValidationResult Validate(CreateOrderRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.UserId <= 0)
        {
            errors["UserId"] = ["UserId must be greater than 0."];
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            errors["Items"] = ["At least one item is required."];
        }
        else
        {
            var itemErrors = new List<string>();
            for (var i = 0; i < request.Items.Count; i++)
            {
                if (request.Items[i].Quantity <= 0)
                {
                    itemErrors.Add($"Items[{i}].Quantity must be greater than 0.");
                }
            }

            if (itemErrors.Count > 0)
            {
                errors["Items"] = [.. itemErrors];
            }
        }

        return errors.Count > 0 ? ValidationResult.WithErrors(errors) : ValidationResult.Success;
    }
}
