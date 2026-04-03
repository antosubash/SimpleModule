using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Database;

namespace SimpleModule.Agents.Module;

[Module(AgentsConstants.ModuleName)]
public class AgentsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<AgentsDbContext>(configuration, AgentsConstants.ModuleName);
        // Override the in-memory session store with EF-backed one
        services.AddScoped<IAgentSessionStore, EfAgentSessionStore>();
    }
}
