using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Core;

/// <summary>
/// Implement this interface to register services in the DI container.
/// Preferred over overriding <see cref="IModule.ConfigureServices"/> on the module class.
/// </summary>
public interface IModuleServices
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}
