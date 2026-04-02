using SimpleModule.Core.Rag;

namespace SimpleModule.Rag.StructuredRag;

internal static class StructurePrompts
{
    internal const string RouterSystem = """
        You are a structure type classifier. Given a user query and document summaries,
        select the most appropriate structure type for organizing the information.

        Structure types:
        - Table: Best for statistical analysis, comparison of quantitative data, feature comparison
        - Graph: Best for entity relationships, long-chain reasoning, dependency tracking
        - Algorithm: Best for planning tasks, procedural steps, workflows
        - Catalogue: Best for summarization, hierarchical organization, categorization
        - Chunk: Best for simple factual questions, single-hop lookups

        Respond with ONLY one word: Table, Graph, Algorithm, Catalogue, or Chunk.
        """;

    internal const string StructurizerTableSystem = """
        Convert the provided documents into a markdown table that captures the key information
        relevant to the user's query. Include appropriate column headers.
        Output ONLY the markdown table, no explanation.
        """;

    internal const string StructurizerGraphSystem = """
        Extract entity-relationship triplets from the provided documents relevant to the user's query.
        Format each triplet as: (Entity1) -[Relationship]-> (Entity2)
        One triplet per line. Output ONLY the triplets, no explanation.
        """;

    internal const string StructurizerAlgorithmSystem = """
        Convert the provided documents into a step-by-step algorithm or procedure
        relevant to the user's query. Use numbered steps with clear actions.
        Output ONLY the algorithm steps, no explanation.
        """;

    internal const string StructurizerCatalogueSystem = """
        Organize the provided documents into a hierarchical catalogue structure
        relevant to the user's query. Use markdown headers and nested bullet points.
        Output ONLY the catalogue, no explanation.
        """;

    internal const string UtilizerSystem = """
        You are a knowledge assistant. Answer the user's question using ONLY the structured
        knowledge provided below. Decompose complex questions into sub-questions, extract
        relevant facts from the structured data, and synthesize a comprehensive answer.

        If the structured knowledge does not contain enough information, say so explicitly.
        """;

    internal static string GetStructurizerPrompt(StructureType type) =>
        type switch
        {
            StructureType.Table => StructurizerTableSystem,
            StructureType.Graph => StructurizerGraphSystem,
            StructureType.Algorithm => StructurizerAlgorithmSystem,
            StructureType.Catalogue => StructurizerCatalogueSystem,
            StructureType.Chunk => "", // No structurization needed for chunks
            _ => "",
        };
}
