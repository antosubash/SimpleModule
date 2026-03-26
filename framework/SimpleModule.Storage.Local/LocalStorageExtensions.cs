using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Storage.Local;

public static class LocalStorageExtensions
{
    public static IServiceCollection AddLocalStorage(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<LocalStorageOptions>(configuration.GetSection("Storage:Local"));
        services.AddSingleton<IStorageProvider, LocalStorageProvider>();
        return services;
    }
}
