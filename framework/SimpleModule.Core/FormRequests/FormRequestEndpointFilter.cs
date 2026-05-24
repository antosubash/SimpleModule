using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Validation;

namespace SimpleModule.Core.FormRequests;

public sealed class FormRequestEndpointFilter : IEndpointFilter
{
    private static readonly JsonSerializerOptions InertiaJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        for (var i = 0; i < context.Arguments.Count; i++)
        {
            if (context.Arguments[i] is not FormRequest formRequest)
                continue;

            if (!formRequest.Authorize(context.HttpContext.User))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: ErrorMessages.ForbiddenTitle,
                    detail: ErrorMessages.DefaultForbiddenMessage
                );
            }

            formRequest.Prepare();

            var result = await formRequest.ValidateAsync(context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                var errors = result.ToValidationErrors();

                if (context.HttpContext.Request.IsInertia())
                {
                    return WriteInertiaValidationError(context.HttpContext, errors);
                }

                return Results.Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: ErrorMessages.ValidationErrorTitle,
                    detail: ErrorMessages.DefaultValidationMessage,
                    extensions: new Dictionary<string, object?> { ["errors"] = errors }
                );
            }
        }

        return await next(context);
    }

    private static InertiaValidationResult WriteInertiaValidationError(
        HttpContext httpContext,
        Dictionary<string, string[]> errors
    )
    {
        var component = "Error/422";
        var pageData = new
        {
            component,
            props = new
            {
                status = 422,
                title = ErrorMessages.ValidationErrorTitle,
                message = ErrorMessages.DefaultValidationMessage,
                errors,
            },
            url = httpContext.Request.Path + httpContext.Request.QueryString,
            version = InertiaMiddleware.Version,
        };

        return new InertiaValidationResult(pageData);
    }

    private sealed class InertiaValidationResult(object pageData) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            httpContext.Response.Headers[InertiaHttpExtensions.InertiaHeader] = "true";
            httpContext.Response.Headers["Vary"] = InertiaHttpExtensions.InertiaHeader;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(
                JsonSerializer.Serialize(pageData, InertiaJsonOptions)
            );
        }
    }
}
