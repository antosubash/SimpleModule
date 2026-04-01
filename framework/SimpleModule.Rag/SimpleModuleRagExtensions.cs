using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Rag;

public static class SimpleModuleRagExtensions
{
    public static IServiceCollection AddSimpleModuleRag(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RagOptions>? configure = null
    )
    {
        services.Configure<RagOptions>(configuration.GetSection("Rag"));
        if (configure is not null)
            services.PostConfigure(configure);

        services.AddSingleton<IKnowledgeStore, VectorKnowledgeStore>();
        services.AddHostedService<KnowledgeIndexingHostedService>();

        return services;
    }
}
