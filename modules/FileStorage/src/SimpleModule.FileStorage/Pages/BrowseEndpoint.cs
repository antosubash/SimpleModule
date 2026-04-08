using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Extensions;
using SimpleModule.Core.Inertia;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.Storage;

namespace SimpleModule.FileStorage.Pages;

public class BrowseEndpoint : IViewEndpoint
{
    public const string Route = FileStorageConstants.Routes.Browse;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (string? folder, HttpContext context, IFileStorageContracts fileStorage) =>
                {
                    var userId = context.User.GetScopedUserId();

                    var files = await fileStorage.GetFilesAsync(folder, userId);
                    var folders = await fileStorage.GetFoldersAsync(folder, userId);

                    var parentFolder = folder is not null
                        ? StoragePathHelper.GetFolder(folder)
                        : null;

                    return Inertia.Render(
                        "FileStorage/Browse",
                        new
                        {
                            files,
                            folders,
                            currentFolder = folder,
                            parentFolder,
                        }
                    );
                }
            )
            .RequirePermission(FileStoragePermissions.View);
    }
}
