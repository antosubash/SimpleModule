namespace SimpleModule.Rag;

public sealed class RagOptions
{
    public int DefaultTopK { get; set; } = 5;
    public float MinScore { get; set; } = 0.7f;
    public int EmbeddingDimension { get; set; } = 1536;
    public bool IndexOnStartup { get; set; } = true;
}
