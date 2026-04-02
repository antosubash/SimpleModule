using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using SimpleModule.Core.Rag;

namespace SimpleModule.Rag;

public sealed class VectorKnowledgeStore(
    VectorStore vectorStore,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator
) : IKnowledgeStore
{
    public async Task IndexDocumentsAsync(
        string collectionName,
        IReadOnlyList<KnowledgeDocument> documents,
        CancellationToken cancellationToken = default
    )
    {
        var collection = vectorStore.GetCollection<string, KnowledgeRecord>(collectionName);
        await collection.EnsureCollectionExistsAsync(cancellationToken);

        var contents = documents.Select(d => d.Content).ToList();
        var embeddings = await embeddingGenerator.GenerateAsync(
            contents,
            cancellationToken: cancellationToken
        );

        // Upsert concurrently in batches for better throughput
        var upsertTasks = new List<Task>(documents.Count);
        for (var i = 0; i < documents.Count; i++)
        {
            var doc = documents[i];
            var record = new KnowledgeRecord
            {
                Id = Guid.NewGuid().ToString(),
                Title = doc.Title,
                Content = doc.Content,
                CollectionName = collectionName,
                ModuleName = doc.Metadata?.GetValueOrDefault("module"),
                Embedding = embeddings[i].Vector,
            };
            upsertTasks.Add(collection.UpsertAsync(record, cancellationToken));
        }

        await Task.WhenAll(upsertTasks);
    }

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        string collectionName,
        string query,
        int topK = 5,
        float minScore = 0.0f,
        CancellationToken cancellationToken = default
    )
    {
        var collection = vectorStore.GetCollection<string, KnowledgeRecord>(collectionName);

        if (!await collection.CollectionExistsAsync(cancellationToken))
            return [];


        var queryEmbeddings = await embeddingGenerator.GenerateAsync(
            [query],
            cancellationToken: cancellationToken
        );
        var queryVector = queryEmbeddings[0].Vector;

        var results = new List<KnowledgeSearchResult>();
        await foreach (
            var result in collection.SearchAsync(
                queryVector,
                top: topK,
                cancellationToken: cancellationToken
            )
        )
        {
            var score = (float)(result.Score ?? 0.0);
            if (score < minScore)
                break; // Results are descending by score; remaining will also be below threshold

            results.Add(
                new KnowledgeSearchResult(
                    result.Record.Title,
                    result.Record.Content,
                    score,
                    result.Record.ModuleName is not null
                        ? new Dictionary<string, string> { ["module"] = result.Record.ModuleName }
                        : null
                )
            );
        }

        return results;
    }

    public async Task DeleteCollectionAsync(
        string collectionName,
        CancellationToken cancellationToken = default
    )
    {
        var collection = vectorStore.GetCollection<string, KnowledgeRecord>(collectionName);
        await collection.EnsureCollectionDeletedAsync(cancellationToken);
    }
}
