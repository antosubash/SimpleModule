using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.Admin;

[Module(AdminConstants.ModuleName, ViewPrefix = "/admin")]
public class AdminModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAdminContracts, AdminService>();
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        // Admin's session-management endpoints inject IOpenIddictSessionContracts, whose
        // implementation lives in SimpleModule.OpenIddict (not its Contracts assembly).
        // Without that module installed, minimal-API parameter binding falls through to
        // body-binding and the host crashes at MapModuleEndpoints with the misleading
        // "Body was inferred but the method does not allow inferred body parameters."
        // Fail fast here with a directive a human can act on.
        // Use IServiceProviderIsService so we don't actually instantiate the (scoped)
        // service from the root provider.
        var probe = app.ApplicationServices.GetRequiredService<IServiceProviderIsService>();
        if (!probe.IsService(typeof(IOpenIddictSessionContracts)))
        {
            throw new InvalidOperationException(
                "SimpleModule.Admin requires SimpleModule.OpenIddict to be installed. "
                    + "Add a reference to the SimpleModule.OpenIddict package (or project) "
                    + "so IOpenIddictSessionContracts can be resolved by Admin's session endpoints."
            );
        }
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Admin",
                Url = "/admin",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.325.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 011.37.49l1.296 2.247a1.125 1.125 0 01-.26 1.431l-1.003.827c-.293.241-.438.613-.43.992a7.723 7.723 0 010 .255c-.008.378.137.75.43.991l1.004.827c.424.35.534.955.26 1.43l-1.298 2.248a1.125 1.125 0 01-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.47 6.47 0 01-.22.128c-.331.183-.581.495-.644.869l-.213 1.281c-.09.543-.56.941-1.11.941h-2.594c-.55 0-1.019-.398-1.11-.94l-.213-1.282c-.062-.373-.312-.686-.644-.87a6.52 6.52 0 01-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 01-1.369-.49l-1.297-2.247a1.125 1.125 0 01.26-1.431l1.004-.827c.292-.24.437-.613.43-.991a6.932 6.932 0 010-.255c.007-.38-.138-.751-.43-.992l-1.004-.827a1.125 1.125 0 01-.26-1.43l1.297-2.247a1.125 1.125 0 011.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.087.22-.128.332-.183.582-.495.644-.869l.214-1.28z"/><path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/></svg>""",
                Order = 85,
                Section = MenuSection.AppSidebar,
                Roles = ["Admin"],
            }
        );
    }
}
