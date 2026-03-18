using Vogen;

namespace SimpleModule.Core.Ids;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct OrderId;
