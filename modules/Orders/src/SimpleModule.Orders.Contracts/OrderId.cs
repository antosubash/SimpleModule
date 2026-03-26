using Vogen;

namespace SimpleModule.Orders.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct OrderId;
