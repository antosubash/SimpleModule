namespace SimpleModule.Core.Validation;

public sealed class ValidationBuilder
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public ValidationBuilder AddErrorIf(bool condition, string field, string message)
    {
        if (condition)
        {
            if (!_errors.TryGetValue(field, out var list))
            {
                list = [];
                _errors[field] = list;
            }

            list.Add(message);
        }

        return this;
    }

    public ValidationResult Build()
    {
        if (_errors.Count == 0)
        {
            return ValidationResult.Success;
        }

        var errors = _errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        return ValidationResult.WithErrors(errors);
    }
}
