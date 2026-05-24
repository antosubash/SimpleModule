using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.Inertia;

public sealed class InertiaErrorResult(
    int statusCode,
    string title,
    string message,
    object? errors = null
) : IResult
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var component = $"Error/{statusCode}";
        var props = errors is not null
            ? (object)
                new
                {
                    status = statusCode,
                    title,
                    message,
                    errors,
                }
            : new
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

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.Headers[InertiaHttpExtensions.InertiaHeader] = "true";
        httpContext.Response.Headers["Vary"] = InertiaHttpExtensions.InertiaHeader;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(pageData, JsonOptions));
    }
}
