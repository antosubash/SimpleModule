using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;
using SimpleModule.Database;

namespace SimpleModule.Chat;

[Module(
    ChatConstants.ModuleName,
    RoutePrefix = ChatConstants.RoutePrefix,
    ViewPrefix = ChatConstants.ViewPrefix
)]
public class ChatModule : IModule, IModuleServices, IModuleMenu
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ChatDbContext>(configuration, ChatConstants.ModuleName);
        services.AddScoped<ChatService>();
        services.AddScoped<IChatContracts>(sp => sp.GetRequiredService<ChatService>());
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<ChatPermissions>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Chat",
                Url = "/chat",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.87 9.87 0 01-4-.8L3 20l1.2-3.6A7.96 7.96 0 013 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"/></svg>""",
                Order = 40,
                Section = MenuSection.AppSidebar,
            }
        );
    }
}
