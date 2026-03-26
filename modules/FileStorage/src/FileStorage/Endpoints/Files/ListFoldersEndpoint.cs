using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class ListFoldersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/folders",
                async (string? parent, IFileStorageContracts files) =>
                    TypedResults.Ok(await files.GetFoldersAsync(parent))
            )
            .RequirePermission(FileStoragePermissions.View);
}
