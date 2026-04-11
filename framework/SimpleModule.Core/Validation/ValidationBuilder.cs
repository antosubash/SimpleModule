using System.Text.RegularExpressions;

namespace SimpleModule.Core.Validation;

public sealed partial class ValidationBuilder
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

    public ValidationBuilder Required(string? value, string field, string? message = null)
    {
        return AddErrorIf(
            string.IsNullOrWhiteSpace(value),
            field,
            message ?? $"{field} is required."
        );
    }

    public ValidationBuilder MaxLength(
        string? value,
        string field,
        int maxLength,
        string? message = null
    )
    {
        return AddErrorIf(
            value is not null && value.Length > maxLength,
            field,
            message ?? $"{field} must be at most {maxLength} characters."
        );
    }

    public ValidationBuilder MinLength(
        string? value,
        string field,
        int minLength,
        string? message = null
    )
    {
        return AddErrorIf(
            value is not null && value.Length < minLength,
            field,
            message ?? $"{field} must be at least {minLength} characters."
        );
    }

    public ValidationBuilder LengthBetween(
        string? value,
        string field,
        int minLength,
        int maxLength,
        string? message = null
    )
    {
        return AddErrorIf(
            value is not null && (value.Length < minLength || value.Length > maxLength),
            field,
            message ?? $"{field} must be between {minLength} and {maxLength} characters."
        );
    }

    public ValidationBuilder MatchesPattern(
        string? value,
        string field,
        string pattern,
        string? message = null
    )
    {
        // Static Regex.IsMatch caches a small set of compiled regexes internally,
        // which is fine for the handful of patterns typical callers use.
        return AddErrorIf(
            value is not null && !Regex.IsMatch(value, pattern, RegexOptions.Compiled),
            field,
            message ?? $"{field} has an invalid format."
        );
    }

    public ValidationBuilder Email(string? value, string field, string? message = null)
    {
        return AddErrorIf(
            !string.IsNullOrWhiteSpace(value) && !EmailRegex().IsMatch(value),
            field,
            message ?? $"{field} must be a valid email address."
        );
    }

    public ValidationBuilder GreaterThan(
        decimal value,
        string field,
        decimal min,
        string? message = null
    )
    {
        return AddErrorIf(value <= min, field, message ?? $"{field} must be greater than {min}.");
    }

    public ValidationBuilder Between(
        decimal value,
        string field,
        decimal min,
        decimal max,
        string? message = null
    )
    {
        return AddErrorIf(
            value < min || value > max,
            field,
            message ?? $"{field} must be between {min} and {max}."
        );
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

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
