using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleModule.Core.Caching;

/// <summary>
/// DI registration for the unified SimpleModule caching abstraction.
/// </summary>
public static class CachingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ICacheStore"/> with the default in-process
    /// <see cref="MemoryCacheStore"/> implementation, along with the underlying
    /// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>. Safe to call
    /// multiple times — registrations are added with <c>TryAdd</c>.
    /// </summary>
    public static IServiceCollection AddSimpleModuleCaching(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMemoryCache();
        services.TryAddSingleton<ICacheStore, MemoryCacheStore>();
        return services;
    }
}
