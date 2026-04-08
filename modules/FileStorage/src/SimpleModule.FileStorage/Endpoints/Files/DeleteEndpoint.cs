using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class DeleteEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.Delete;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
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

                    await files.DeleteFileAsync(file);
                    return Results.NoContent();
                }
            )
            .RequirePermission(FileStoragePermissions.Delete);
}
