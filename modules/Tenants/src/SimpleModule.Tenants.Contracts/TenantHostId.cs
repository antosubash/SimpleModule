using Vogen;

namespace SimpleModule.Tenants.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct TenantHostId;
