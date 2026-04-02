using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Rag;
using SimpleModule.Rag.StructuredRag.Preprocessing;

namespace SimpleModule.Rag.StructuredRag;

public static class StructuredRagExtensions
{
    public static IServiceCollection AddStructuredRag(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<StructuredRagOptions>? configure = null
    )
    {
        if (configuration is not null)
            services.Configure<StructuredRagOptions>(configuration.GetSection("Rag:StructuredRag"));
        if (configure is not null)
            services.PostConfigure(configure);

        services.AddScoped<IStructureRouter, LlmStructureRouter>();
        services.AddScoped<IKnowledgeStructurizer, LlmKnowledgeStructurizer>();
        services.AddScoped<IStructuredKnowledgeUtilizer, LlmStructuredKnowledgeUtilizer>();
        services.AddScoped<IRagPipeline, StructuredRagPipeline>();

        // Preprocessor (requires IStructuredKnowledgeCache to be registered by the cache module)
        services.AddScoped<IKnowledgePreprocessor, LlmKnowledgePreprocessor>();

        return services;
    }
}
