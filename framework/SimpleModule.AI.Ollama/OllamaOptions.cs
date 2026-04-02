namespace SimpleModule.AI.Ollama;

public sealed class OllamaOptions
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
}
