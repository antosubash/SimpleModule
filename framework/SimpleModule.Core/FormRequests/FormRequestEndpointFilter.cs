using Microsoft.AspNetCore.Http;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Validation;

namespace SimpleModule.Core.FormRequests;

public sealed class FormRequestEndpointFilter : IEndpointFilter
{
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
                if (context.HttpContext.Request.IsInertia())
                {
                    return new InertiaErrorResult(
                        StatusCodes.Status403Forbidden,
                        ErrorMessages.ForbiddenTitle,
                        ErrorMessages.DefaultForbiddenMessage
                    );
                }

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
                    return new InertiaErrorResult(
                        StatusCodes.Status422UnprocessableEntity,
                        ErrorMessages.ValidationErrorTitle,
                        ErrorMessages.DefaultValidationMessage,
                        errors
                    );
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
}
