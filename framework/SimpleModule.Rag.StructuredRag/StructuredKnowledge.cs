using SimpleModule.Core.Rag;

namespace SimpleModule.Rag.StructuredRag;

public sealed record StructuredKnowledge(StructureType Type, string Content, string RawQuery);
