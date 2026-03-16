using SimpleModule.Core.Constants;

namespace SimpleModule.Core.Exceptions;

public sealed class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base(ErrorMessages.DefaultValidationMessage)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(Dictionary<string, string[]> errors)
        : base(ErrorMessages.DefaultValidationMessage)
    {
        Errors = errors;
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }
}
