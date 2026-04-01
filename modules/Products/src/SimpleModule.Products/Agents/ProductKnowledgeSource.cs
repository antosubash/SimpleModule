using SimpleModule.Core.Rag;

namespace SimpleModule.Products.Agents;

public class ProductKnowledgeSource : IKnowledgeSource
{
    public string CollectionName => "product-knowledge";

    public Task<IReadOnlyList<KnowledgeDocument>> GetDocumentsAsync(
        CancellationToken cancellationToken
    ) =>
        Task.FromResult<IReadOnlyList<KnowledgeDocument>>([
            new(
                "Pricing Rules",
                "Products are priced in USD. Bulk discounts apply for orders over 100 units. "
                    + "Discounts exceeding 50% require manager approval.",
                new Dictionary<string, string> { ["module"] = "Products" }
            ),
            new(
                "Return Policy",
                "Products can be returned within 30 days with the original receipt. "
                    + "Digital products are non-refundable after download.",
                new Dictionary<string, string> { ["module"] = "Products" }
            ),
        ]);
}
