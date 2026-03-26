using Vogen;

namespace SimpleModule.PageBuilder.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct PageTagId;
