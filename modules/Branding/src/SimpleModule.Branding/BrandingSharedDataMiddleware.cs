using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Branding;

/// <summary>
/// Publishes the resolved <see cref="BrandingDto"/> as the <c>branding</c> Inertia
/// shared prop so React chrome (app name, logo, top bar, footer) can render it on
/// every page.
/// </summary>
public sealed class BrandingSharedDataMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IBrandingContracts branding)
    {
        var sharedData = context.RequestServices.GetService<InertiaSharedData>();
        if (sharedData is not null)
            sharedData.Set("branding", await branding.GetBrandingAsync());

        await next(context);
    }
}
