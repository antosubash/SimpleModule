using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Blazor.Inertia;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Blazor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimpleModuleBlazor(
        this IServiceCollection services,
        Action<InertiaOptions>? configure = null
    )
    {
        services.AddScoped<IInertiaPageRenderer, InertiaPageRenderer>();
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<InertiaOptions>(_ => { });
        return services;
    }
}
