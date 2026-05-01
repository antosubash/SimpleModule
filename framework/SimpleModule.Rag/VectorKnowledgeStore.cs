using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using SimpleModule.Core.Rag;

namespace SimpleModule.Rag;

public sealed class VectorKnowledgeStore : IKnowledgeStore
{
    public const int DefaultEmbeddingDimension = 1536;

    private readonly VectorStore _vectorStore;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly VectorStoreCollectionDefinition _definition;

    public VectorKnowledgeStore(
        VectorStore vectorStore,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IOptions<RagOptions> options
    )
    {
        _vectorStore = vectorStore;
        _embeddingGenerator = embeddingGenerator;
        _definition = BuildDefinition(options.Value.EmbeddingDimension);
    }

    private static VectorStoreCollectionDefinition BuildDefinition(int dimension) =>
        new()
        {
            Properties =
            {
                new VectorStoreKeyProperty("Id", typeof(string)),
                new VectorStoreDataProperty("Title", typeof(string)),
                new VectorStoreDataProperty("Content", typeof(string)),
                new VectorStoreDataProperty("CollectionName", typeof(string)),
                new VectorStoreDataProperty("ModuleName", typeof(string)) { IsIndexed = true },
                new VectorStoreVectorProperty(
                    "Embedding",
                    typeof(ReadOnlyMemory<float>),
                    dimension
                ),
            },
        };

    public async Task IndexDocumentsAsync(
        string collectionName,
        IReadOnlyList<KnowledgeDocument> documents,
        CancellationToken cancellationToken = default
    )
    {
        var collection = _vectorStore.GetCollection<string, KnowledgeRecord>(
            collectionName,
            _definition
        );
        await collection.EnsureCollectionExistsAsync(cancellationToken);

        var contents = documents.Select(d => d.Content).ToList();
        var embeddings = await _embeddingGenerator.GenerateAsync(
            contents,
            cancellationToken: cancellationToken
        );

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
        var collection = _vectorStore.GetCollection<string, KnowledgeRecord>(
            collectionName,
            _definition
        );

        if (!await collection.CollectionExistsAsync(cancellationToken))
            return [];

        var queryEmbeddings = await _embeddingGenerator.GenerateAsync(
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
        var collection = _vectorStore.GetCollection<string, KnowledgeRecord>(
            collectionName,
            _definition
        );
        await collection.EnsureCollectionDeletedAsync(cancellationToken);
    }
}
