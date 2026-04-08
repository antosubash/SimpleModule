using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class GetByIdEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.GetById;
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

                    return Results.Ok(file);
                }
            )
            .RequirePermission(FileStoragePermissions.View);
}
