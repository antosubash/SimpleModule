using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.Inertia;

public interface IInertiaPageRenderer
{
    Task RenderPageAsync(HttpContext httpContext, string pageJson);
}
