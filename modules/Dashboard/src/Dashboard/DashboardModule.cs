using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Menu;

namespace SimpleModule.Dashboard;

[Module(DashboardConstants.ModuleName, RoutePrefix = DashboardConstants.RoutePrefix)]
public class DashboardModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }

    public void ConfigureMenu(IMenuBuilder menus) { }
}
