using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Rag;
using SimpleModule.Rag.StructuredRag.Data;

namespace SimpleModule.Rag.StructuredRag.Preprocessing;

public sealed partial class LlmKnowledgePreprocessor(
    IKnowledgeStructurizer structurizer,
    IStructuredKnowledgeCache cache,
    IOptions<StructuredRagOptions> options,
    ILogger<LlmKnowledgePreprocessor> logger
) : IKnowledgePreprocessor
{
    // Structure types to preprocess (skip Chunk — it's a passthrough)
    private static readonly StructureType[] StructureTypes =
    [
        StructureType.Table,
        StructureType.Graph,
        StructureType.Algorithm,
        StructureType.Catalogue,
    ];

    public async Task PreprocessAsync(
        string collectionName,
        IReadOnlyList<KnowledgeDocument> documents,
        CancellationToken cancellationToken = default
    )
    {
        if (documents.Count == 0)
            return;

        var docContents = documents.Select(d => d.Content).ToList();
        var expiry = options.Value.PreprocessedCacheTtl is { } ttl
            ? DateTimeOffset.UtcNow.Add(ttl)
            : (DateTimeOffset?)null;

        foreach (var structureType in StructureTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if already cached for this content
            var contentHash = ContentHasher.ComputeHash(docContents);
            var existing = await cache.GetAsync(
                collectionName,
                contentHash,
                structureType,
                cancellationToken
            );
            if (existing is not null)
            {
                LogSkippingCached(logger, collectionName, structureType);
                continue;
            }

            try
            {
                LogPreprocessing(logger, collectionName, structureType, documents.Count);

                var structured = await structurizer.StructurizeAsync(
                    structureType,
                    $"Preprocess documents for {structureType} format",
                    docContents,
                    cancellationToken
                );

                var entry = new CachedStructuredKnowledge
                {
                    CollectionName = collectionName,
                    DocumentHash = contentHash,
                    StructureType = structureType,
                    StructuredContent = structured.Content,
                    SourceTitle = string.Join(", ", documents.Select(d => d.Title)),
                    ExpiresAt = expiry,
                };

                await cache.SaveAsync(entry, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogPreprocessingFailed(logger, ex, collectionName, structureType);
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Preprocessing {CollectionName} into {StructureType} format ({DocumentCount} docs)"
    )]
    private static partial void LogPreprocessing(
        ILogger logger,
        string collectionName,
        StructureType structureType,
        int documentCount
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping {CollectionName}/{StructureType} — already cached"
    )]
    private static partial void LogSkippingCached(
        ILogger logger,
        string collectionName,
        StructureType structureType
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to preprocess {CollectionName} into {StructureType}"
    )]
    private static partial void LogPreprocessingFailed(
        ILogger logger,
        Exception ex,
        string collectionName,
        StructureType structureType
    );
}
