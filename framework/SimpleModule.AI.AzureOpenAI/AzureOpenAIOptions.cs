namespace SimpleModule.AI.AzureOpenAI;

public sealed class AzureOpenAIOptions
{
    public string Endpoint { get; set; } = "";
    public string DeploymentName { get; set; } = "gpt-4o";
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-3-small";
    public string ApiKey { get; set; } = "";
}
