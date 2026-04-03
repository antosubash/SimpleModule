using Vogen;

namespace SimpleModule.RateLimiting.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct RateLimitRuleId;
