using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Core.Exceptions;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private static readonly JsonSerializerOptions InertiaJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                ErrorMessages.ValidationErrorTitle,
                null
            ),
            NotFoundException => (StatusCodes.Status404NotFound, ErrorMessages.NotFoundTitle, null),
            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                ErrorMessages.ForbiddenTitle,
                null
            ),
            ConflictException => (StatusCodes.Status409Conflict, ErrorMessages.ConflictTitle, null),
            _ => (
                StatusCodes.Status500InternalServerError,
                ErrorMessages.InternalServerErrorTitle,
                null
            ),
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

        var detail =
            statusCode == StatusCodes.Status500InternalServerError
                ? ErrorMessages.UnexpectedError
                : exception.Message;

        httpContext.Response.StatusCode = statusCode;

        // Inertia requests get an Inertia error page response
        if (httpContext.Request.Headers.ContainsKey("X-Inertia"))
        {
            return await WriteInertiaErrorAsync(httpContext, statusCode, title, detail);
        }

        // API/non-Inertia requests get ProblemDetails JSON
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

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static async ValueTask<bool> WriteInertiaErrorAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string message
    )
    {
        var component = $"Error/{statusCode}";
        var props = new
        {
            status = statusCode,
            title,
            message,
        };

        var pageData = new
        {
            component,
            props,
            url = httpContext.Request.Path + httpContext.Request.QueryString,
            version = InertiaMiddleware.Version,
        };

        httpContext.Response.Headers["X-Inertia"] = "true";
        httpContext.Response.Headers["Vary"] = "X-Inertia";
        httpContext.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(pageData, InertiaJsonOptions);
        await httpContext.Response.WriteAsync(json);
        return true;
    }
}
