using Vogen;

namespace SimpleModule.Map.Contracts;

[ValueObject<Guid>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct BasemapId;
