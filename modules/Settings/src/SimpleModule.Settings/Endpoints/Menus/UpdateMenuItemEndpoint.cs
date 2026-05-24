using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.FormRequests;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class UpdateMenuItemEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.UpdateMenuItem;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async Task<IResult> (
                    int id,
                    UpdateMenuItemFormRequest request,
                    PublicMenuService service
                ) =>
                {
                    var dto = new UpdateMenuItemRequest
                    {
                        Label = request.Label,
                        Url = request.Url,
                        PageRoute = request.PageRoute,
                        Icon = request.Icon,
                        CssClass = request.CssClass,
                        OpenInNewTab = request.OpenInNewTab,
                        IsVisible = request.IsVisible,
                        IsHomePage = request.IsHomePage,
                    };
                    var entity = await service.UpdateAsync(PublicMenuItemId.From(id), dto);
                    return entity is not null ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .RequireAuthorization();
}
