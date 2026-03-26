using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Storage.Azure;

public static class AzureBlobStorageExtensions
{
    public static IServiceCollection AddAzureBlobStorage(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<AzureBlobStorageOptions>(configuration.GetSection("Storage:Azure"));
        services.AddSingleton<IStorageProvider, AzureBlobStorageProvider>();
        return services;
    }
}
