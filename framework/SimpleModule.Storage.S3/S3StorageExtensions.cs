using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Storage.S3;

public static class S3StorageExtensions
{
    public static IServiceCollection AddS3Storage(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<S3StorageOptions>(configuration.GetSection("Storage:S3"));
        services.AddSingleton<IStorageProvider, S3StorageProvider>();
        return services;
    }
}
