using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class DeleteEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.Delete;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                (FileStorageId id, IFileStorageContracts files) =>
                    CrudEndpoints.Delete(() => files.DeleteFileAsync(id))
            )
            .RequirePermission(FileStoragePermissions.Delete);
}
