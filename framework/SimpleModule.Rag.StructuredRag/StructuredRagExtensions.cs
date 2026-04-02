using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Rag;
using SimpleModule.Database;
using SimpleModule.Rag.StructuredRag.Data;
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

        services.AddSingleton<IStructureRouter, LlmStructureRouter>();
        services.AddSingleton<IKnowledgeStructurizer, LlmKnowledgeStructurizer>();
        services.AddSingleton<IStructuredKnowledgeUtilizer, LlmStructuredKnowledgeUtilizer>();
        services.AddSingleton<IRagPipeline, StructuredRagPipeline>();

        // Database-backed cache
        services.AddScoped<IStructuredKnowledgeCache, EfStructuredKnowledgeCache>();
        services.AddScoped<IKnowledgePreprocessor, LlmKnowledgePreprocessor>();

        // Register StructuredRag DbContext (uses module DbContext pattern)
        if (configuration is not null)
        {
            services.AddModuleDbContext<StructuredRagDbContext>(configuration, "StructuredRag");
        }

        return services;
    }
}
