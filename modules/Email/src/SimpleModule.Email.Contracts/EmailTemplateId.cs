using Vogen;

namespace SimpleModule.Email.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct EmailTemplateId;
