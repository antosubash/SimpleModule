using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Extensions;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Endpoints.Notifications;

public class ListNotificationsEndpoint : IEndpoint
{
    public const string Route = NotificationsConstants.Routes.List;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (
                    [AsParameters] QueryNotificationsRequest request,
                    HttpContext context,
                    INotificationsContracts notifications
                ) =>
                    notifications.ListAsync(UserId.From(context.User.GetUserId()!), request)
            )
            .RequirePermission(NotificationsPermissions.ViewOwn);
}
