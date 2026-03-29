namespace SimpleModule.Marketplace.Contracts;

public class MarketplaceSearchRequest
{
    public string? Query { get; set; }
    public MarketplaceCategory? Category { get; set; }
    public MarketplaceSortOption SortBy { get; set; } = MarketplaceSortOption.Relevance;
    public int Skip { get; set; }
    public int Take { get; set; } = 20;
}
