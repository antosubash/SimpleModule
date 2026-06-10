using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Core.Extensions;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Services;
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
                    INotificationStore store,
                    INotificationsContracts notifications,
                    IAuthorizer authorizer
                ) =>
                {
                    // Load → authorize → act: the permission gate below stays the coarse
                    // capability check; NotificationPolicy owns the per-instance rule.
                    var notification = await store.FindAsync(NotificationId.From(id));
                    if (notification is null)
                    {
                        return TypedResults.NotFound();
                    }

                    await authorizer.AuthorizeAsync(
                        context.User,
                        NotificationPolicy.MarkRead,
                        notification,
                        context.RequestAborted
                    );

                    // The contract call stays owner-scoped (defense in depth); false here
                    // means the notification vanished between load and write.
                    var ok = await notifications.MarkReadAsync(
                        notification.Id,
                        UserId.From(context.User.GetUserId()!)
                    );
                    return ok ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .RequirePermission(NotificationsPermissions.ViewOwn);
}
