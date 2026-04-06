using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class ListFoldersEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.ListFolders;
    public const string Method = "GET";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (string? parent, IFileStorageContracts files) =>
                    TypedResults.Ok(await files.GetFoldersAsync(parent))
            )
            .RequirePermission(FileStoragePermissions.View);
}
