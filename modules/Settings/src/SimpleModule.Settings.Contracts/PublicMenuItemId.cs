using Vogen;

namespace SimpleModule.Settings.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct PublicMenuItemId;
