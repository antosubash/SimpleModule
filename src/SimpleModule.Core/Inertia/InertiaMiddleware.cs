using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.Inertia;

public static class InertiaMiddleware
{
    public static string Version { get; set; } = "1";

    public static IApplicationBuilder UseInertia(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Inertia-Version"] = Version;

            if (context.Request.Headers.ContainsKey("X-Inertia")
                && context.Request.Method == "GET"
                && context.Request.Headers["X-Inertia-Version"].FirstOrDefault() != Version)
            {
                context.Response.StatusCode = 409;
                context.Response.Headers["X-Inertia-Location"] = context.Request.GetEncodedUrl();
                return;
            }

            await next();
        });
    }

    private static string GetEncodedUrl(this HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
    }
}
