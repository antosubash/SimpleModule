namespace SimpleModule.Core.Rag;

/// <summary>
/// The structured format used by StructRAG to represent knowledge.
/// The hybrid structure router selects the optimal type per query.
/// </summary>
public enum StructureType
{
    /// <summary>Markdown table format, best for statistical analysis and comparison tasks.</summary>
    Table,

    /// <summary>Entity-relationship triplets (head-relation-tail), best for long-chain reasoning.</summary>
    Graph,

    /// <summary>Pseudocode or procedural steps, best for planning tasks.</summary>
    Algorithm,

    /// <summary>Hierarchical organization, best for summarization tasks.</summary>
    Catalogue,

    /// <summary>Traditional text chunks, best for simple single-hop questions.</summary>
    Chunk,
}
