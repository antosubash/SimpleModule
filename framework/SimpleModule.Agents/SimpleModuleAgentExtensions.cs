using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Agents;

public static class SimpleModuleAgentExtensions
{
    public static IServiceCollection AddSimpleModuleAgents(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AgentOptions>? configure = null
    )
    {
        services.Configure<AgentOptions>(configuration.GetSection("Agents"));
        if (configure is not null)
            services.PostConfigure(configure);

        services.AddScoped<AgentChatService>();

        return services;
    }
}
