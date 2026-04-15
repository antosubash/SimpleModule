using System.Globalization;
using System.Text;
using FluentValidation;
using FluentValidation.Results;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public sealed class CreateRequestValidator : AbstractValidator<CreateOrderRequest>
{
    private static readonly CompositeFormat QuantityMustBePositiveFormat = CompositeFormat.Parse(
        OrdersConstants.ValidationMessages.QuantityMustBePositiveFormat
    );

    public CreateRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithName(OrdersConstants.Fields.UserId)
            .WithMessage(OrdersConstants.ValidationMessages.UserIdRequired);

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithName(OrdersConstants.Fields.Items)
            .WithMessage(OrdersConstants.ValidationMessages.AtLeastOneItemRequired);

        // Items[i].Quantity > 0 — emitted under the "Items" field so callers see
        // a single aggregated errors-by-field dictionary, matching the prior
        // ValidationBuilder shape.
        RuleFor(x => x.Items)
            .Custom(
                (items, context) =>
                {
                    for (var i = 0; i < items.Count; i++)
                    {
                        if (items[i].Quantity <= 0)
                        {
                            context.AddFailure(
                                new ValidationFailure(
                                    OrdersConstants.Fields.Items,
                                    string.Format(
                                        CultureInfo.InvariantCulture,
                                        QuantityMustBePositiveFormat,
                                        i
                                    )
                                )
                            );
                        }
                    }
                }
            )
            .When(x => x.Items is { Count: > 0 });
    }
}
