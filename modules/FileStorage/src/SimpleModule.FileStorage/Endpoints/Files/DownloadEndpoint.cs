using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class DownloadEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.Download;
    public const string Method = "GET";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (FileStorageId id, HttpContext context, IFileStorageContracts files) =>
                {
                    var file = await files.GetFileByIdAsync(id);
                    if (file is null)
                    {
                        return Results.NotFound();
                    }

                    if (!FileOwnershipCheck.CanAccess(file, context.User))
                    {
                        return Results.Forbid();
                    }

                    var stream = await files.DownloadFileAsync(file);
                    return stream is null
                        ? Results.NotFound()
                        : TypedResults.File(stream, file.ContentType, file.FileName);
                }
            )
            .RequirePermission(FileStoragePermissions.View);
}
