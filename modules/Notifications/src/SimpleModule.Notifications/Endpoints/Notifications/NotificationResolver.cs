using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Services;

namespace SimpleModule.Notifications.Endpoints.Notifications;

/// <summary>
/// Loads notifications for the declarative <c>AuthorizeResource&lt;Notification&gt;</c>
/// endpoint filter (see <see cref="MarkReadEndpoint"/>).
/// </summary>
internal sealed class NotificationResolver(INotificationStore store)
    : IResourceResolver<Notification>
{
    public async ValueTask<Notification?> ResolveAsync(
        string routeValue,
        CancellationToken cancellationToken = default
    ) =>
        Guid.TryParse(routeValue, out var id)
            ? await store.FindAsync(NotificationId.From(id), cancellationToken)
            : null;
}
