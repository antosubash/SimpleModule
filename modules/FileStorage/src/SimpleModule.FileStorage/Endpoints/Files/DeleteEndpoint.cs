using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class DeleteEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.Delete;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
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
                        PolicyActions.Delete,
                        file,
                        context.RequestAborted
                    );

                    await files.DeleteFileAsync(file);
                    return Results.NoContent();
                }
            )
            .RequirePermission(FileStoragePermissions.Delete);
}
