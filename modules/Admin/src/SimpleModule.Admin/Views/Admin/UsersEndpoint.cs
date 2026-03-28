using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Views.Admin;

[ViewPage("Admin/Admin/Users")]
public class UsersEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/users",
                async (
                    IUserAdminContracts userAdmin,
                    IOptions<AdminModuleOptions> options,
                    string? search,
                    int page = 1
                ) =>
                {
                    var pageSize = options.Value.UsersPageSize;
                    var result = await userAdmin.GetUsersPagedAsync(search, page, pageSize);
                    var totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);

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
