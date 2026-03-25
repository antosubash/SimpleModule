using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;

namespace SimpleModule.Core;

public interface IModule
{
    virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
    virtual void ConfigureMiddleware(IApplicationBuilder app) { }
    virtual void ConfigureMenu(IMenuBuilder menus) { }
    virtual void ConfigurePermissions(PermissionRegistryBuilder builder) { }
    virtual void ConfigureSettings(ISettingsBuilder settings) { }
}
