using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Extensions;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class GetAllEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.GetAll;
    public const string Method = "GET";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (string? folder, HttpContext context, IFileStorageContracts files) =>
                {
                    var userId = context.User.GetScopedUserId();
                    return CrudEndpoints.GetAll(() => files.GetFilesAsync(folder, userId));
                }
            )
            .RequirePermission(FileStoragePermissions.View);
}
