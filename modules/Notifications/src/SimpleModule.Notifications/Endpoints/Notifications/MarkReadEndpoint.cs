using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Core.Extensions;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Endpoints.Notifications;

public class MarkReadEndpoint : IEndpoint
{
    public const string Route = NotificationsConstants.Routes.MarkRead;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async Task<IResult> (
                    Guid id,
                    HttpContext context,
                    INotificationsContracts notifications
                ) =>
                {
                    // AuthorizeResource already loaded the notification and ran
                    // NotificationPolicy; the contract call stays owner-scoped
                    // (defense in depth), so false means it vanished meanwhile.
                    var ok = await notifications.MarkReadAsync(
                        NotificationId.From(id),
                        UserId.From(context.User.GetUserId()!)
                    );
                    return ok ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .RequirePermission(NotificationsPermissions.ViewOwn)
            .AuthorizeResource<Notification>(NotificationPolicy.MarkRead);
}
