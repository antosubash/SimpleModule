using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.Storage;

namespace SimpleModule.FileStorage.Views;

public class BrowseEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/browse",
                async (string? folder, IFileStorageContracts fileStorage) =>
                {
                    var files = await fileStorage.GetFilesAsync(folder);
                    var folders = await fileStorage.GetFoldersAsync(folder);

                    string? parentFolder = null;
                    if (folder is not null)
                    {
                        var normalized = StoragePathHelper.Normalize(folder);
                        var lastSlash = normalized.LastIndexOf('/');
                        parentFolder = lastSlash > 0 ? normalized[..lastSlash] : null;
                    }

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
