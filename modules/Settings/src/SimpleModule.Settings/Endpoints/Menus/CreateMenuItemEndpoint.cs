using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.FormRequests;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class CreateMenuItemEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.CreateMenuItem;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (CreateMenuItemFormRequest request, PublicMenuService service) =>
                {
                    var dto = new CreateMenuItemRequest
                    {
                        ParentId = request.ParentId,
                        Label = request.Label,
                        Url = request.Url,
                        PageRoute = request.PageRoute,
                        Icon = request.Icon,
                        CssClass = request.CssClass,
                        OpenInNewTab = request.OpenInNewTab,
                        IsVisible = request.IsVisible,
                        IsHomePage = request.IsHomePage,
                    };
                    var entity = await service.CreateAsync(dto);
                    var result = new PublicMenuItemDto
                    {
                        Id = entity.Id,
                        ParentId = entity.ParentId,
                        Label = entity.Label,
                        Url = entity.Url,
                        PageRoute = entity.PageRoute,
                        Icon = entity.Icon,
                        CssClass = entity.CssClass,
                        OpenInNewTab = entity.OpenInNewTab,
                        IsVisible = entity.IsVisible,
                        IsHomePage = entity.IsHomePage,
                        SortOrder = entity.SortOrder,
                    };
                    return TypedResults.Created($"/api/settings/menus/{entity.Id}", result);
                }
            )
            .RequirePermission(SettingsPermissions.ManageMenus);
}
