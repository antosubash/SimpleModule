using Vogen;

namespace SimpleModule.FeatureFlags.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct FeatureFlagOverrideId;
