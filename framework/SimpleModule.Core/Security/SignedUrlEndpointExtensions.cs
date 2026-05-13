using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Core.Security;

public static class SignedUrlEndpointExtensions
{
    public const string SignedUrlClaimsItemKey = "SimpleModule.SignedUrl.Claims";

    public static TBuilder RequireSignedUrl<TBuilder>(this TBuilder builder, string? purpose = null)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AllowAnonymous();
        builder.AddEndpointFilter(
            async (context, next) =>
            {
                var generator =
                    context.HttpContext.RequestServices.GetRequiredService<ISignedUrlGenerator>();

                if (!generator.TryValidate(context.HttpContext.Request, purpose, out var claims))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                context.HttpContext.Items[SignedUrlClaimsItemKey] = claims;
                return await next(context);
            }
        );
        return builder;
    }
}
