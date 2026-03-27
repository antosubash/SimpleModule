using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Views.Admin;

[ViewPage("Admin/Admin/Users")]
public class UsersEndpoint : IViewEndpoint
{
    private const int PageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/users",
                async (IUserAdminContracts userAdmin, string? search, int page = 1) =>
                {
                    var result = await userAdmin.GetUsersPagedAsync(search, page, PageSize);
                    var totalPages = (int)Math.Ceiling((double)result.TotalCount / PageSize);

                    return Inertia.Render(
                        "Admin/Admin/Users",
                        new
                        {
                            users = result.Items,
                            search = search ?? "",
                            page = result.Page,
                            totalPages,
                            totalCount = result.TotalCount,
                        }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
