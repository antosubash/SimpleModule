using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class GetByIdEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.GetById;
    public const string Method = "GET";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (FileStorageId id, IFileStorageContracts files) =>
                    CrudEndpoints.GetById(() => files.GetFileByIdAsync(id))
            )
            .RequirePermission(FileStoragePermissions.View);
}
