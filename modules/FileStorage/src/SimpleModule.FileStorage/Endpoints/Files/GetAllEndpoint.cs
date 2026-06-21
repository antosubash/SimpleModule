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
                (
                    string? folder,
                    int? skip,
                    int? take,
                    HttpContext context,
                    IFileStorageContracts files
                ) =>
                {
                    var userId = context.User.GetScopedUserId();
                    return CrudEndpoints.GetAll(() =>
                        files.GetFilesAsync(
                            folder,
                            userId,
                            Math.Max(0, skip ?? 0),
                            Math.Clamp(take ?? 30, 1, 200)
                        )
                    );
                }
            )
            .RequirePermission(FileStoragePermissions.View);
}
