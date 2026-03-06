namespace SimpleModule.Core.Exceptions;

public sealed class ConflictException : Exception
{
    public ConflictException()
        : base("A conflict occurred.") { }

    public ConflictException(string message)
        : base(message) { }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException) { }
}
