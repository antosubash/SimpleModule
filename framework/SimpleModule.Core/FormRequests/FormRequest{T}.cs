using System.Collections.Concurrent;
using FluentValidation;
using FluentValidation.Results;

namespace SimpleModule.Core.FormRequests;

public abstract class FormRequest<TSelf> : FormRequest
    where TSelf : FormRequest<TSelf>
{
    private static readonly ConcurrentDictionary<Type, InlineValidator<TSelf>> ValidatorCache =
        new();

    protected abstract void ConfigureRules(RuleConfigurator<TSelf> rules);

    internal sealed override async Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken
    )
    {
        var validator = ValidatorCache.GetOrAdd(
            typeof(TSelf),
            _ =>
            {
                var configurator = new RuleConfigurator<TSelf>();
                ConfigureRules(configurator);
                return configurator.Build();
            }
        );

        return await validator.ValidateAsync((TSelf)this, cancellationToken);
    }
}
