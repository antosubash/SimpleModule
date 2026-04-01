using System.Diagnostics;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Rag;

namespace SimpleModule.Rag.StructuredRag;

public sealed class StructuredRagPipeline(
    IKnowledgeStore knowledgeStore,
    IStructureRouter structureRouter,
    IKnowledgeStructurizer knowledgeStructurizer,
    IStructuredKnowledgeUtilizer knowledgeUtilizer,
    IOptions<RagOptions> ragOptions
) : IRagPipeline
{
    public async Task<RagResult> QueryAsync(
        string query,
        RagQueryOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();

        var topK = options?.TopK ?? ragOptions.Value.DefaultTopK;
        var minScore = options?.MinScore ?? ragOptions.Value.MinScore;

        // Stage 0: Retrieve documents via vector search
        var searchResults = await knowledgeStore.SearchAsync(
            "default",
            query,
            topK,
            minScore,
            cancellationToken
        );

        if (searchResults.Count == 0)
        {
            return new RagResult(
                "No relevant documents found.",
                [],
                new RagMetadata("StructuredRag", null, stopwatch.Elapsed)
            );
        }

        var documents = searchResults.Select(r => r.Content).ToList();
        var summaries = searchResults
            .Select(r => r.Content.Length > 200 ? r.Content[..200] + "..." : r.Content)
            .ToList();

        // Stage 1: Route — select the optimal structure type
        var structureType =
            options?.ForceStructure
            ?? await structureRouter.SelectStructureAsync(query, summaries, cancellationToken);

        // Stage 2: Structurize — convert documents to chosen format
        var structuredKnowledge = await knowledgeStructurizer.StructurizeAsync(
            structureType,
            query,
            documents,
            cancellationToken
        );

        // Stage 3: Utilize — reason over structured data to produce answer
        var answer = await knowledgeUtilizer.AnswerAsync(
            query,
            structuredKnowledge,
            cancellationToken
        );

        stopwatch.Stop();

        var sources = searchResults
            .Select(r => new RagSource(r.Title, r.Content, r.Score, r.Metadata))
            .ToList();

        return new RagResult(
            answer,
            sources,
            new RagMetadata("StructuredRag", structureType, stopwatch.Elapsed)
        );
    }
}
