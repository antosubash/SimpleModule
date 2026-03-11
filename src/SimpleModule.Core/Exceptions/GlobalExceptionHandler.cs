using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Constants;

namespace SimpleModule.Core.Exceptions;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                ErrorMessages.ValidationErrorTitle,
                ve.Errors
            ),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                ErrorMessages.NotFoundTitle,
                (Dictionary<string, string[]>?)null
            ),
            ConflictException => (StatusCodes.Status409Conflict, ErrorMessages.ConflictTitle, null),
            _ => (StatusCodes.Status500InternalServerError, ErrorMessages.InternalServerErrorTitle, null),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            logger.LogWarning(
                exception,
                "Handled exception occurred: {Message}",
                exception.Message
            );
        }

        var detail = statusCode == StatusCodes.Status500InternalServerError
            ? ErrorMessages.UnexpectedError
            : exception.Message;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
        };

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
