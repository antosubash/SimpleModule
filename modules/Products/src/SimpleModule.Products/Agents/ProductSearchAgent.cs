using SimpleModule.Core.Agents;

namespace SimpleModule.Products.Agents;

public class ProductSearchAgent : IAgentDefinition
{
    public string Name => "product-search";

    public string Description => "Searches and answers questions about products";

    public string Instructions =>
        """
            You are a product search assistant. Use the available tools to find and describe products.
            Always provide prices when available. When comparing products, present data clearly.
            """;

    public string? RagCollectionName => "product-knowledge";
}
