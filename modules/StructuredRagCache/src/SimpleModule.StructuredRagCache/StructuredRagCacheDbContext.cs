using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Rag.StructuredRag.Data;

namespace SimpleModule.StructuredRagCache;

public sealed class StructuredRagCacheDbContext(
    DbContextOptions<StructuredRagCacheDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<CachedStructuredKnowledge> CachedStructuredKnowledge =>
        Set<CachedStructuredKnowledge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(
            new EntityConfigurations.CachedStructuredKnowledgeConfiguration()
        );
        modelBuilder.ApplyModuleSchema(StructuredRagCacheConstants.ModuleName, dbOptions.Value);
    }
}
