using Vogen;

namespace SimpleModule.AuditLogs.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct AuditEntryId;
