using SimpleModule.Core.Constants;

namespace SimpleModule.Core.Exceptions;

public sealed class ForbiddenException : Exception
{
    public ForbiddenException()
        : base(ErrorMessages.DefaultForbiddenMessage) { }

    public ForbiddenException(string message)
        : base(message) { }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException) { }
}
