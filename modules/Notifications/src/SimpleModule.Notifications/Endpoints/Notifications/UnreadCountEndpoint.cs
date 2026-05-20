using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Extensions;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Endpoints.Notifications;

public class UnreadCountEndpoint : IEndpoint
{
    public const string Route = NotificationsConstants.Routes.UnreadCount;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (
                    HttpContext context,
                    INotificationsContracts notifications,
                    CancellationToken ct
                ) =>
                {
                    var count = await notifications.GetUnreadCountAsync(
                        UserId.From(context.User.GetUserId()!),
                        ct
                    );
                    return new { count };
                }
            )
            .RequirePermission(NotificationsPermissions.ViewOwn);
}
