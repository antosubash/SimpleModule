using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Extensions;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Features.Notifications.MarkAllRead;

public class MarkAllReadEndpoint : IEndpoint
{
    public const string Route = NotificationsConstants.Routes.MarkAllRead;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (HttpContext context, INotificationsContracts notifications) =>
                {
                    var marked = await notifications.MarkAllReadAsync(
                        UserId.From(context.User.GetUserId()!)
                    );
                    return new { marked };
                }
            )
            .RequirePermission(NotificationsPermissions.ViewOwn);
}
