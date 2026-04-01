using Microsoft.AspNetCore.Builder;

namespace SimpleModule.Core;

/// <summary>
/// Implement this interface to configure ASP.NET middleware for the module.
/// Preferred over overriding <see cref="IModule.ConfigureMiddleware"/> on the module class.
/// </summary>
public interface IModuleMiddleware
{
    void ConfigureMiddleware(IApplicationBuilder app);
}
