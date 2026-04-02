using Vogen;

namespace SimpleModule.BackgroundJobs.Contracts;

[ValueObject<Guid>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct RecurringJobId;
