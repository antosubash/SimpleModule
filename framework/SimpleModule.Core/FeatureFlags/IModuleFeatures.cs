namespace SimpleModule.Core.FeatureFlags;

/// <summary>
/// Marker interface for feature flag classes. Implementations are auto-discovered
/// by the source generator and registered with the feature flag registry.
/// Feature classes must be sealed and contain only public const string fields.
/// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces
public interface IModuleFeatures;
#pragma warning restore CA1040
