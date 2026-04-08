using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Extensions;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class UploadEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.Upload;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async Task<IResult> (
                    IFormFile? file,
                    string? folder,
                    HttpContext context,
                    IFileStorageContracts files
                ) =>
                {
                    if (file is null || file.Length == 0)
                    {
                        return TypedResults.BadRequest("A file is required.");
                    }

                    var userId = context.User.GetUserId();
                    await using var stream = file.OpenReadStream();
                    var storedFile = await files.UploadFileAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        folder,
                        userId
                    );
                    return TypedResults.Created($"/api/files/{storedFile.Id}", storedFile);
                }
            )
            .RequirePermission(FileStoragePermissions.Upload)
            .DisableAntiforgery();
}
