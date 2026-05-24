using FluentValidation;
using FluentValidation.Results;

namespace SimpleModule.Core.FormRequests;

public abstract class FormRequest<TSelf> : FormRequest
    where TSelf : FormRequest<TSelf>
{
    private static volatile InlineValidator<TSelf>? _cachedValidator;

    protected abstract void ConfigureRules(RuleConfigurator<TSelf> rules);

    internal sealed override async Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken
    )
    {
        var validator = _cachedValidator;
        if (validator is null)
        {
            var configurator = new RuleConfigurator<TSelf>();
            ConfigureRules(configurator);
            validator = configurator.Build();
            Interlocked.CompareExchange(ref _cachedValidator, validator, null);
            validator = _cachedValidator;
        }

        return await validator.ValidateAsync((TSelf)this, cancellationToken);
    }
}
