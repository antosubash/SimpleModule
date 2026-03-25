using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class CreateMenuItemEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/menus",
                async (CreateMenuItemRequest request, PublicMenuService service) =>
                {
                    var entity = await service.CreateAsync(request);
                    var dto = new PublicMenuItemDto
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
                    return TypedResults.Created($"/api/settings/menus/{entity.Id}", dto);
                }
            )
            .RequireAuthorization();
}
