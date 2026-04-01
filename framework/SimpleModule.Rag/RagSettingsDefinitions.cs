using SimpleModule.Core.Settings;

namespace SimpleModule.Rag;

public static class RagSettingsDefinitions
{
    public static void Register(ISettingsBuilder settings)
    {
        settings
            .Add(
                new SettingDefinition
                {
                    Key = "Rag.DefaultTopK",
                    DisplayName = "Default Top-K Results",
                    Description = "Number of documents to retrieve per query",
                    Group = "RAG (Retrieval-Augmented Generation)",
                    Type = SettingType.Number,
                    Scope = SettingScope.Application,
                    DefaultValue = "5",
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "Rag.MinScore",
                    DisplayName = "Minimum Similarity Score",
                    Description = "Minimum vector similarity threshold (0.0-1.0)",
                    Group = "RAG (Retrieval-Augmented Generation)",
                    Type = SettingType.Number,
                    Scope = SettingScope.Application,
                    DefaultValue = "0.7",
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "Rag.IndexOnStartup",
                    DisplayName = "Index on Startup",
                    Description =
                        "Whether to index knowledge documents when the application starts",
                    Group = "RAG (Retrieval-Augmented Generation)",
                    Type = SettingType.Bool,
                    Scope = SettingScope.Application,
                    DefaultValue = "true",
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "Rag.StructuredRag.EnableRouter",
                    DisplayName = "Enable Structure Router",
                    Description =
                        "Enable StructRAG hybrid router to select optimal format per query",
                    Group = "RAG (Retrieval-Augmented Generation)",
                    Type = SettingType.Bool,
                    Scope = SettingScope.Application,
                    DefaultValue = "true",
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "Rag.StructuredRag.DefaultStructure",
                    DisplayName = "Default Structure Type",
                    Description =
                        "Fallback structure type when router is disabled (Table, Graph, Algorithm, Catalogue, Chunk)",
                    Group = "RAG (Retrieval-Augmented Generation)",
                    Type = SettingType.Text,
                    Scope = SettingScope.Application,
                    DefaultValue = "Chunk",
                }
            );
    }
}
