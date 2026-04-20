using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimpleModule.DevTools;

public static class DevToolsExtensions
{
    /// <summary>
    /// Registers development-time services including Vite file watching and Tailwind CSS rebuilds.
    /// Only call this in Development environments.
    /// </summary>
    public static IServiceCollection AddDevTools(this IServiceCollection services)
    {
        services.AddSingleton<LiveReloadServer>();
        return services;
    }
}
