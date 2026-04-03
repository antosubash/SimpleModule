using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.Agents;
using SimpleModule.Agents.Guardrails;
using SimpleModule.Agents.Middleware;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Database;

namespace SimpleModule.Agents.Module;

[Module(AgentsConstants.ModuleName)]
public class AgentsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<AgentsDbContext>(configuration, AgentsConstants.ModuleName);
        services.AddScoped<IAgentSessionStore, EfAgentSessionStore>();

        // Middleware
        services.AddSingleton<IAgentMiddleware, LoggingMiddleware>();
        services.AddSingleton<IAgentMiddleware, RateLimitingMiddleware>();
        services.AddSingleton<IAgentMiddleware, TokenTrackingMiddleware>();
        services.AddSingleton<IAgentMiddleware, RetryMiddleware>();

        // Guardrails
        services.AddSingleton<IAgentGuardrail, ContentLengthGuardrail>();
        services.AddSingleton<IAgentGuardrail, PiiRedactionGuardrail>();
        services.AddSingleton<IAgentGuardrail, PromptInjectionGuardrail>();

        // File service
        services.AddScoped<AgentFileService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var registry = endpoints.ServiceProvider.GetRequiredService<IAgentRegistry>();
        AgentEndpoints.MapAgentEndpoints(endpoints, registry);

        var env = endpoints.ServiceProvider.GetService<IWebHostEnvironment>();
        if (env?.IsDevelopment() == true)
        {
            AgentPlaygroundEndpoints.Map(endpoints);
        }
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        AgentSettingsDefinitions.Register(settings);
    }
}
