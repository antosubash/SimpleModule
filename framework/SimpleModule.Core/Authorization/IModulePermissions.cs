namespace SimpleModule.Core.Authorization;

/// <summary>
/// Marker interface for permission classes. Implementations are auto-discovered
/// by the source generator and registered with the permission registry.
/// Permission classes must be sealed and contain only public const string fields.
/// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces
public interface IModulePermissions;
#pragma warning restore CA1040
