using SimpleModule.Core.FeatureFlags;

namespace SimpleModule.Products;

public sealed class ProductsFeatures : IModuleFeatures
{
    public const string BulkImport = "Products.BulkImport";
    public const string AdvancedPricing = "Products.AdvancedPricing";
}
