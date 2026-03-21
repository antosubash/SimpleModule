using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Database;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Permissions.Services;

namespace SimpleModule.Permissions;

[Module(PermissionsConstants.ModuleName)]
public class PermissionsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<PermissionsDbContext>(
            configuration,
            PermissionsConstants.ModuleName
        );

        services.AddHostedService<PermissionSeedService>();
    }
}
