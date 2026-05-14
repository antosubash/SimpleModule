using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Extensions;
using SimpleModule.Core.Inertia;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Contracts.Features.Notifications.List;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Pages;

public class InboxEndpoint : IViewEndpoint
{
    public const string Route = NotificationsConstants.Routes.Inbox;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (
                    HttpContext context,
                    INotificationsContracts notifications,
                    bool? unreadOnly
                ) =>
                {
                    var userId = UserId.From(context.User.GetUserId()!);
                    var page = await notifications.ListAsync(
                        userId,
                        new QueryNotificationsRequest { UnreadOnly = unreadOnly }
                    );
                    var unreadCount = await notifications.GetUnreadCountAsync(userId);

                    return Inertia.Render(
                        "Notifications/Inbox",
                        new
                        {
                            items = page.Items,
                            totalCount = page.TotalCount,
                            unreadCount,
                        }
                    );
                }
            )
            .RequirePermission(NotificationsPermissions.ViewOwn);
}
