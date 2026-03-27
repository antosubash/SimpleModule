using System.Diagnostics.CodeAnalysis;

namespace SimpleModule.Core;

/// <summary>
/// Represents the outcome of an operation that may fail with a structured error.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Standard Result<T> pattern requires static factory methods")]
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly T? _value;
    private readonly ResultError? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(ResultError error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    public ResultError Error => !IsSuccess
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    public static Result<T> Ok(T value) => new(value);

    public static Result<T> Fail(string message) => new(new ResultError(message));

    public static Result<T> Fail(string message, Dictionary<string, string[]> validationErrors) =>
        new(new ResultError(message, validationErrors));

    /// <summary>
    /// Creates a <see cref="Result{T}"/> from a value (alternate for implicit operator).
    /// </summary>
    [SuppressMessage("Design", "CA2225:Operator overloads have named alternates", Justification = "ToResult provided")]
    public static implicit operator Result<T>(T value) => Ok(value);

    /// <summary>
    /// Named alternate for the implicit conversion operator.
    /// </summary>
    public static Result<T> FromValue(T value) => Ok(value);

    /// <summary>
    /// Maps the success value to a new type using the provided function.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess
            ? Result<TOut>.Ok(map(_value!))
            : Result<TOut>.Fail(Error.Message, Error.ValidationErrors ?? new Dictionary<string, string[]>());

    /// <summary>
    /// Returns the value if successful, or the provided fallback value.
    /// </summary>
    public T ValueOr(T fallback) => IsSuccess ? _value! : fallback;

    public bool Equals(Result<T> other) =>
        IsSuccess == other.IsSuccess
        && EqualityComparer<T?>.Default.Equals(_value, other._value)
        && Equals(_error, other._error);

    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(IsSuccess, _value, _error);

    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);
}

/// <summary>
/// Describes why an operation failed.
/// </summary>
public sealed record ResultError(
    string Message,
    Dictionary<string, string[]>? ValidationErrors = null
);
