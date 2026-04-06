using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
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
                async (string? folder, IFileStorageContracts fileStorage) =>
                {
                    var filesTask = fileStorage.GetFilesAsync(folder);
                    var foldersTask = fileStorage.GetFoldersAsync(folder);

                    var files = await filesTask;
                    var folders = await foldersTask;

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
