using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Database;
using SimpleModule.Items.Contracts;

namespace SimpleModule.Items;

[Module(ItemsConstants.ModuleName, RoutePrefix = ItemsConstants.RoutePrefix, ViewPrefix = "/")]
public class ItemsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ItemsDbContext>(configuration, ItemsConstants.ModuleName);
        services.AddScoped<IItemContracts, ItemService>();
    }
}
