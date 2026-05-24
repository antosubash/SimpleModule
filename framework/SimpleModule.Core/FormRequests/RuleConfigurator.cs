using System.Linq.Expressions;
using FluentValidation;

namespace SimpleModule.Core.FormRequests;

public sealed class RuleConfigurator<T>
    where T : class
{
    private readonly InlineValidator<T> _validator = new();

    public IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(
        Expression<Func<T, TProperty>> expression
    ) => _validator.RuleFor(expression);

    public IRuleBuilderInitialCollection<T, TProperty> RuleForEach<TProperty>(
        Expression<Func<T, IEnumerable<TProperty>>> expression
    ) => _validator.RuleForEach(expression);

    internal InlineValidator<T> Build() => _validator;
}
