using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder;

[Module(
    PageBuilderConstants.ModuleName,
    RoutePrefix = PageBuilderConstants.RoutePrefix,
    ViewPrefix = PageBuilderConstants.ViewPrefix
)]
public class PageBuilderModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<PageBuilderDbContext>(
            configuration,
            PageBuilderConstants.ModuleName
        );
        services.AddValidatorsFromAssemblyContaining<PageBuilderModule>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Pages",
                Url = "/pages",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z"/></svg>""",
                Order = 40,
                Section = MenuSection.AppSidebar,
                Roles = ["Admin"],
            }
        );
    }
}
