using SimpleModule.Marketplace.Contracts;

namespace SimpleModule.Marketplace;

public static class CategoryMapper
{
    private static readonly Dictionary<string, MarketplaceCategory> TagCategoryMap = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["auth"] = MarketplaceCategory.Auth,
        ["authentication"] = MarketplaceCategory.Auth,
        ["authorization"] = MarketplaceCategory.Auth,
        ["identity"] = MarketplaceCategory.Auth,
        ["openiddict"] = MarketplaceCategory.Auth,
        ["oauth"] = MarketplaceCategory.Auth,
        ["storage"] = MarketplaceCategory.Storage,
        ["blob"] = MarketplaceCategory.Storage,
        ["file-storage"] = MarketplaceCategory.Storage,
        ["s3"] = MarketplaceCategory.Storage,
        ["azure-storage"] = MarketplaceCategory.Storage,
        ["ui"] = MarketplaceCategory.UI,
        ["components"] = MarketplaceCategory.UI,
        ["blazor"] = MarketplaceCategory.UI,
        ["react"] = MarketplaceCategory.UI,
        ["analytics"] = MarketplaceCategory.Analytics,
        ["reporting"] = MarketplaceCategory.Analytics,
        ["dashboard"] = MarketplaceCategory.Analytics,
        ["integration"] = MarketplaceCategory.Integration,
        ["api"] = MarketplaceCategory.Integration,
        ["webhook"] = MarketplaceCategory.Integration,
        ["email"] = MarketplaceCategory.Communication,
        ["sms"] = MarketplaceCategory.Communication,
        ["notification"] = MarketplaceCategory.Communication,
        ["messaging"] = MarketplaceCategory.Communication,
        ["monitoring"] = MarketplaceCategory.Monitoring,
        ["logging"] = MarketplaceCategory.Monitoring,
        ["health-check"] = MarketplaceCategory.Monitoring,
        ["audit"] = MarketplaceCategory.Monitoring,
    };

    public static MarketplaceCategory MapCategory(IEnumerable<string> tags)
    {
        foreach (var tag in tags)
        {
            if (TagCategoryMap.TryGetValue(tag, out var category))
            {
                return category;
            }
        }

        return MarketplaceCategory.Other;
    }
}
