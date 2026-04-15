using FluentValidation.Results;

namespace SimpleModule.Core.Validation;

/// <summary>
/// Bridges FluentValidation's <see cref="ValidationResult"/> to the
/// <see cref="Dictionary{TKey,TValue}"/> shape consumed by
/// <see cref="SimpleModule.Core.Exceptions.ValidationException"/> and
/// the RFC 7807 <c>errors</c> extension written by <c>GlobalExceptionHandler</c>.
/// </summary>
public static class ValidationResultExtensions
{
    /// <summary>
    /// Groups validation failures by property name, flattening their error
    /// messages into a <c>string[]</c> per field.
    /// </summary>
    public static Dictionary<string, string[]> ToValidationErrors(this ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result
            .Errors.GroupBy(e => e.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray(),
                StringComparer.Ordinal
            );
    }
}
