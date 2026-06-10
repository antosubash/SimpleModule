using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class DownloadEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.Download;
    public const string Method = "GET";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (
                    FileStorageId id,
                    HttpContext context,
                    IFileStorageContracts files,
                    IAuthorizer authorizer
                ) =>
                {
                    var file = await files.GetFileByIdAsync(id);
                    if (file is null)
                    {
                        return Results.NotFound();
                    }

                    // Owner-or-admin; FileStoragePolicy denies non-owners as 404.
                    await authorizer.AuthorizeAsync(
                        context.User,
                        PolicyActions.View,
                        file,
                        context.RequestAborted
                    );

                    var stream = await files.DownloadFileAsync(file);
                    return stream is null
                        ? Results.NotFound()
                        : TypedResults.File(stream, file.ContentType, file.FileName);
                }
            )
            .RequirePermission(FileStoragePermissions.View);
}
