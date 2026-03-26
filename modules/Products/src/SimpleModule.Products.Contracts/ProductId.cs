using Vogen;

namespace SimpleModule.Products.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct ProductId;
