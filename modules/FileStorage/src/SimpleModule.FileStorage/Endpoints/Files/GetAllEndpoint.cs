using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class GetAllEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.GetAll;
    public const string Method = "GET";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (string? folder, IFileStorageContracts files) =>
                    CrudEndpoints.GetAll(() => files.GetFilesAsync(folder))
            )
            .RequirePermission(FileStoragePermissions.View);
}
