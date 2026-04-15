namespace SimpleModule.Core.Constants;

public static class ErrorMessages
{
    // Exception titles (used in ProblemDetails)
    public const string ValidationErrorTitle = "Validation Error";
    public const string NotFoundTitle = "Not Found";
    public const string ConflictTitle = "Conflict";
    public const string ForbiddenTitle = "Forbidden";
    public const string InternalServerErrorTitle = "Internal Server Error";

    // Default exception messages
    public const string UnexpectedError = "An unexpected error occurred. Please try again later.";
    public const string DefaultValidationMessage = "One or more validation errors occurred.";
    public const string DefaultNotFoundMessage = "The requested resource was not found.";
    public const string DefaultConflictMessage = "A conflict occurred.";
    public const string DefaultForbiddenMessage =
        "You do not have permission to access this resource.";
}
