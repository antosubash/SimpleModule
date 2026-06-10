using SimpleModule.Notifications.Contracts;

namespace SimpleModule.Notifications.Services;

/// <summary>
/// Module-internal loader for the load → authorize → act flow. Deliberately NOT on
/// <see cref="INotificationsContracts"/>: the unscoped read exists only so the policy
/// check can see the resource before the handler acts — cross-module callers keep the
/// owner-scoped contract surface.
/// </summary>
internal interface INotificationStore
{
    Task<Notification?> FindAsync(
        NotificationId id,
        CancellationToken cancellationToken = default
    );
}
