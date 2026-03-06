namespace SimpleModule.Core.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException()
        : base("The requested resource was not found.") { }

    public NotFoundException(string message)
        : base(message) { }

    public NotFoundException(string entityName, object id)
        : base($"{entityName} with ID {id} not found") { }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}
