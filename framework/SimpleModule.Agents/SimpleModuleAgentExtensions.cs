using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Agents.Guardrails;
using SimpleModule.Agents.Middleware;
using SimpleModule.Agents.Sessions;

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
        services.AddSingleton<IAgentSessionStore, InMemoryAgentSessionStore>();

        // Middleware
        services.AddSingleton<LoggingMiddleware>();
        services.AddSingleton<RateLimitingMiddleware>();
        services.AddSingleton<TokenTrackingMiddleware>();
        services.AddSingleton<RetryMiddleware>();

        // Guardrails
        services.AddSingleton<IAgentGuardrail, ContentLengthGuardrail>();
        services.AddSingleton<IAgentGuardrail, PiiRedactionGuardrail>();
        services.AddSingleton<IAgentGuardrail, PromptInjectionGuardrail>();

        // File handling
        services.AddScoped<Files.AgentFileService>();

        return services;
    }

    public static WebApplication UseAgentDevTools(this WebApplication app)
    {
        DevTools.AgentPlaygroundEndpoints.Map(app);
        return app;
    }
}
