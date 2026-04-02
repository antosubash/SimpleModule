using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Database;
using SimpleModule.Rag.StructuredRag.Data;

namespace SimpleModule.StructuredRagCache;

[Module(StructuredRagCacheConstants.ModuleName)]
public class StructuredRagCacheModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<StructuredRagCacheDbContext>(
            configuration,
            StructuredRagCacheConstants.ModuleName
        );
        services.AddScoped<IStructuredKnowledgeCache, EfStructuredKnowledgeCache>();
    }
}
