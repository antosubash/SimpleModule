using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Rag;
using SimpleModule.Rag.StructuredRag.Data;

namespace SimpleModule.Rag.Module;

public sealed class EfStructuredKnowledgeCache(RagDbContext db) : IStructuredKnowledgeCache
{
    public async Task<CachedStructuredKnowledge?> GetAsync(
        string collectionName,
        string documentHash,
        StructureType structureType,
        CancellationToken cancellationToken = default
    )
    {
        return await db.CachedStructuredKnowledge.FirstOrDefaultAsync(
            e =>
                e.CollectionName == collectionName
                && e.DocumentHash == documentHash
                && e.StructureType == structureType
                && (e.ExpiresAt == null || e.ExpiresAt > DateTimeOffset.UtcNow),
            cancellationToken
        );
    }

    public async Task<IReadOnlyList<CachedStructuredKnowledge>> GetByCollectionAsync(
        string collectionName,
        StructureType structureType,
        CancellationToken cancellationToken = default
    )
    {
        return await db
            .CachedStructuredKnowledge.Where(e =>
                e.CollectionName == collectionName
                && e.StructureType == structureType
                && (e.ExpiresAt == null || e.ExpiresAt > DateTimeOffset.UtcNow)
            )
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(
        CachedStructuredKnowledge entry,
        CancellationToken cancellationToken = default
    )
    {
        await UpsertEntryAsync(entry, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveBatchAsync(
        IReadOnlyList<CachedStructuredKnowledge> entries,
        CancellationToken cancellationToken = default
    )
    {
        foreach (var entry in entries)
        {
            await UpsertEntryAsync(entry, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertEntryAsync(
        CachedStructuredKnowledge entry,
        CancellationToken cancellationToken
    )
    {
        var existing = await db.CachedStructuredKnowledge.FirstOrDefaultAsync(
            e =>
                e.CollectionName == entry.CollectionName
                && e.DocumentHash == entry.DocumentHash
                && e.StructureType == entry.StructureType,
            cancellationToken
        );

        if (existing is not null)
        {
            existing.StructuredContent = entry.StructuredContent;
            existing.CreatedAt = DateTimeOffset.UtcNow;
            existing.ExpiresAt = entry.ExpiresAt;
            existing.HitCount = 0;
        }
        else
        {
            db.CachedStructuredKnowledge.Add(entry);
        }
    }

    public async Task CleanExpiredAsync(CancellationToken cancellationToken = default)
    {
        await db
            .CachedStructuredKnowledge.Where(e =>
                e.ExpiresAt != null && e.ExpiresAt <= DateTimeOffset.UtcNow
            )
            .ExecuteDeleteAsync(cancellationToken);
    }
}
